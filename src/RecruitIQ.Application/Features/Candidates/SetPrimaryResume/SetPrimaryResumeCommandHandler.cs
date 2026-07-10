using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Candidates.SetPrimaryResume;

public class SetPrimaryResumeCommandHandler : IRequestHandler<SetPrimaryResumeCommand, Result<bool>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public SetPrimaryResumeCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(SetPrimaryResumeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Load the target resume
        var targetResume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId && r.CandidateId == request.CandidateId && r.CompanyId == companyId, cancellationToken);

        if (targetResume == null)
        {
            return Result<bool>.Failure("Resume not found or does not belong to the candidate.");
        }

        // 2. Set all other resumes of this candidate as non-primary
        var otherResumes = await _context.Resumes
            .Where(r => r.CandidateId == request.CandidateId && r.Id != request.ResumeId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var resume in otherResumes)
        {
            if (resume.IsPrimary)
            {
                resume.IsPrimary = false;
                _context.Update(resume);
            }
        }

        // 3. Mark target as primary
        targetResume.IsPrimary = true;
        _context.Update(targetResume);

        // 4. Log Activity
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Action = $"Primary Resume Set: {targetResume.OriginalFileName}",
            EntityName = "Candidate",
            EntityId = request.CandidateId,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        // 5. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
