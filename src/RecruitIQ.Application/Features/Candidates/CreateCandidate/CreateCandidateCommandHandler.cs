using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.CreateCandidate;

public class CreateCandidateCommandHandler : IRequestHandler<CreateCandidateCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateCandidateCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Validate email uniqueness within the current tenant (handled by global query filter automatically)
        var emailExists = await _context.Candidates
            .AnyAsync(c => c.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            return Result<Guid>.Failure($"A candidate with email '{request.Email}' already exists in your workspace.");
        }

        // 2. Generate Candidate Number: CAN-{Year}-{Sequence:D5}
        var candidateCount = await _context.Candidates
            .CountAsync(c => c.CompanyId == companyId, cancellationToken);
        var candidateNumber = $"CAN-{DateTime.UtcNow.Year}-{(candidateCount + 1):D5}";

        // 3. Map and add Candidate entity (defaulting Status to New)
        var candidate = new Candidate
        {
            CompanyId = companyId,
            CandidateNumber = candidateNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            LinkedInUrl = request.LinkedInUrl,
            Title = request.Title,
            YearsOfExperience = request.YearsOfExperience,
            Status = CandidateStatus.New
        };

        _context.Add(candidate);

        // 3. Log Activity "Candidate Created"
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Action = "Candidate Created",
            EntityName = "Candidate",
            EntityId = candidate.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        // 4. Persist Changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(candidate.Id);
    }
}
