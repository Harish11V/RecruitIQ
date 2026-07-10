using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.ChangeCandidateStatus;

public class ChangeCandidateStatusCommandHandler : IRequestHandler<ChangeCandidateStatusCommand, Result<byte[]>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICandidateLifecycleService _lifecycleService;
    private readonly ICurrentUserService _currentUserService;

    public ChangeCandidateStatusCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICandidateLifecycleService lifecycleService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _lifecycleService = lifecycleService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<byte[]>> Handle(ChangeCandidateStatusCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Load candidate with tracking
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId && c.CompanyId == companyId, cancellationToken);

        if (candidate == null)
        {
            return Result<byte[]>.Failure("CandidateNotFound");
        }

        // 2. Validate lifecycle transition
        if (!_lifecycleService.IsTransitionAllowed(candidate.Status, request.NewStatus))
        {
            return Result<byte[]>.Failure("InvalidCandidateStatusTransition");
        }

        // 3. Set original RowVersion for EF optimistic concurrency
        _context.SetOriginalRowVersion(candidate, request.RowVersion);

        // 4. Update status and log activity if changed
        if (candidate.Status != request.NewStatus)
        {
            var oldStatusStr = candidate.Status.ToString();
            var newStatusStr = request.NewStatus.ToString();

            candidate.Status = request.NewStatus;

            Guid? currentUserId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            var activity = new Activity
            {
                CompanyId = companyId,
                UserId = currentUserId,
                Action = $"Candidate Status Changed: {oldStatusStr} → {newStatusStr}",
                EntityName = "Candidate",
                EntityId = candidate.Id,
                Timestamp = DateTime.UtcNow
            };
            _context.Add(activity);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw; // Caught by controller to return HTTP 409 Conflict
            }
        }

        return Result<byte[]>.Success(candidate.RowVersion);
    }
}
