using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Candidates.UploadResume;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public UploadResumeCommandHandler(
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

    public async Task<Result<Guid>> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Verify candidate exists and belongs to the current tenant
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId && c.CompanyId == companyId, cancellationToken);

        if (candidate == null)
        {
            return Result<Guid>.Failure("Candidate not found.");
        }

        // 2. Prevent duplicate uploads of the same file name for this candidate
        var duplicateExists = await _context.Resumes
            .AnyAsync(r => r.CandidateId == request.CandidateId && r.OriginalFileName == request.FileName, cancellationToken);

        if (duplicateExists)
        {
            return Result<Guid>.Failure($"A resume with name '{request.FileName}' has already been uploaded for this candidate.");
        }

        // 3. Upload file to physical storage
        var uniqueFileName = $"{Guid.NewGuid()}_{request.FileName}";
        var storageUrl = await _fileStorage.UploadFileAsync(
            request.FileStream,
            uniqueFileName,
            request.MimeType,
            cancellationToken);

        // 4. Check if we should mark this resume as primary (if no primary resume exists)
        var hasPrimary = await _context.Resumes
            .AnyAsync(r => r.CandidateId == request.CandidateId && r.IsPrimary, cancellationToken);

        var resume = new Resume
        {
            CompanyId = companyId,
            CandidateId = request.CandidateId,
            FileName = Path.GetFileName(storageUrl),
            OriginalFileName = request.FileName,
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            StoragePath = storageUrl,
            UploadedDate = DateTime.UtcNow,
            Version = 1,
            ParserVersion = "v1.0.0",
            IsPrimary = !hasPrimary
        };

        _context.Add(resume);

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
            Action = $"Resume Uploaded: {request.FileName} (Size: {request.FileSize / 1024} KB)",
            EntityName = "Candidate",
            EntityId = candidate.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        // 6. Save DB record
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(resume.Id);
    }
}
