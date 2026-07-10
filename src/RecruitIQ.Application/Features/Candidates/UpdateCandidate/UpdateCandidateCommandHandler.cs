using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.UpdateCandidate;

public class UpdateCandidateCommandHandler : IRequestHandler<UpdateCandidateCommand, Result<byte[]>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCandidateCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<byte[]>> Handle(UpdateCandidateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Fetch Candidate with tracking
        var candidate = await _context.QueryReadOnly<Candidate>() // Wait! We should use a tracked query. Let's see if EF context tracks default queries
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.CompanyId == companyId, cancellationToken);

        // Wait! QueryReadOnly<Candidate>() is mapped as AsNoTracking().
        // If we want tracking, we should query from EF DbContext directly!
        // Let's check if we can query from _context.Candidates (which is IQueryable<Candidate>).
        // Let's get the tracked candidate entity:
        var trackedCandidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.CompanyId == companyId, cancellationToken);

        if (trackedCandidate == null)
        {
            return Result<byte[]>.Failure("Candidate not found.");
        }

        // 2. Set original RowVersion for EF optimistic concurrency
        _context.SetOriginalRowVersion(trackedCandidate, request.RowVersion);

        // 3. Track fields that changed for activity logging
        var changedFields = new System.Collections.Generic.List<string>();
        if (trackedCandidate.FirstName != request.FirstName)
        {
            changedFields.Add($"First Name: '{trackedCandidate.FirstName}' -> '{request.FirstName}'");
            trackedCandidate.FirstName = request.FirstName;
        }
        if (trackedCandidate.LastName != request.LastName)
        {
            changedFields.Add($"Last Name: '{trackedCandidate.LastName}' -> '{request.LastName}'");
            trackedCandidate.LastName = request.LastName;
        }
        if (trackedCandidate.Email != request.Email)
        {
            // Verify email uniqueness if email changed
            var emailExists = await _context.Candidates
                .AnyAsync(c => c.Id != request.Id && c.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                return Result<byte[]>.Failure($"A candidate with email '{request.Email}' already exists in your workspace.");
            }
            changedFields.Add($"Email: '{trackedCandidate.Email}' -> '{request.Email}'");
            trackedCandidate.Email = request.Email;
        }
        if (trackedCandidate.PhoneNumber != request.PhoneNumber)
        {
            changedFields.Add($"Phone Number: '{trackedCandidate.PhoneNumber ?? "None"}' -> '{request.PhoneNumber ?? "None"}'");
            trackedCandidate.PhoneNumber = request.PhoneNumber;
        }
        if (trackedCandidate.LinkedInUrl != request.LinkedInUrl)
        {
            changedFields.Add($"LinkedIn URL: '{trackedCandidate.LinkedInUrl ?? "None"}' -> '{request.LinkedInUrl ?? "None"}'");
            trackedCandidate.LinkedInUrl = request.LinkedInUrl;
        }
        if (trackedCandidate.Title != request.Title)
        {
            changedFields.Add($"Title: '{trackedCandidate.Title ?? "None"}' -> '{request.Title ?? "None"}'");
            trackedCandidate.Title = request.Title;
        }
        if (trackedCandidate.Status != request.Status)
        {
            changedFields.Add($"Status: '{trackedCandidate.Status}' -> '{request.Status}'");
            trackedCandidate.Status = request.Status;
        }
        if (trackedCandidate.YearsOfExperience != request.YearsOfExperience)
        {
            changedFields.Add($"Experience: '{trackedCandidate.YearsOfExperience?.ToString() ?? "None"}' -> '{request.YearsOfExperience?.ToString() ?? "None"}'");
            trackedCandidate.YearsOfExperience = request.YearsOfExperience;
        }

        // 4. Save and log activity if changes occurred
        if (changedFields.Any())
        {
            Guid? currentUserId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            var actionText = $"Candidate Updated: {trackedCandidate.FirstName} {trackedCandidate.LastName} ({trackedCandidate.CandidateNumber}). Changed fields: {string.Join(", ", changedFields)}";
            if (actionText.Length > 100)
            {
                actionText = actionText[..97] + "...";
            }

            var activity = new Activity
            {
                CompanyId = companyId,
                UserId = currentUserId,
                Action = actionText,
                EntityName = "Candidate",
                EntityId = trackedCandidate.Id,
                Timestamp = DateTime.UtcNow
            };
            _context.Add(activity);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Rethrow so it is caught in the controller as a 409 Conflict
                throw;
            }
        }

        return Result<byte[]>.Success(trackedCandidate.RowVersion);
    }
}
