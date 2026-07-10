using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Candidates.DeleteResume;

public class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand, Result<bool>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public DeleteResumeCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        IFileStorageService fileStorage,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Fetch tracked Resume
        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId && r.CandidateId == request.CandidateId && r.CompanyId == companyId, cancellationToken);

        if (resume == null)
        {
            return Result<bool>.Failure("Resume not found or does not belong to the candidate.");
        }

        // 2. Delete file physically
        if (!string.IsNullOrEmpty(resume.StoragePath))
        {
            try
            {
                await _fileStorage.DeleteFileAsync(resume.StoragePath, cancellationToken);
            }
            catch (Exception)
            {
                // Soft warning, continue to remove database entry
            }
        }

        // 3. Re-assign primary badge to another resume if this primary is deleted
        if (resume.IsPrimary)
        {
            var otherResume = await _context.Resumes
                .FirstOrDefaultAsync(r => r.CandidateId == request.CandidateId && r.Id != request.ResumeId && !r.IsDeleted, cancellationToken);
            if (otherResume != null)
            {
                otherResume.IsPrimary = true;
                _context.Update(otherResume);
            }
        }

        // 4. Remove database record (EF soft deletes automatically via interceptor)
        _context.Remove(resume);

        // 5. Log Activity
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Action = $"Resume Deleted: {resume.OriginalFileName}",
            EntityName = "Candidate",
            EntityId = request.CandidateId,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        // 6. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
