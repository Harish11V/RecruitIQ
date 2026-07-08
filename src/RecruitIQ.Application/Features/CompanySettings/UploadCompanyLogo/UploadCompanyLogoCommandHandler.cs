using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.CompanySettings.UploadCompanyLogo;

public class UploadCompanyLogoCommandHandler : IRequestHandler<UploadCompanyLogoCommand, Result<string>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

    public UploadCompanyLogoCommandHandler(
        IRecruitIQDbContext context, 
        ITenantService tenantService, 
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<string>> Handle(UploadCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Validate File Extension
        var extension = Path.GetExtension(request.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result<string>.Failure($"Invalid file extension. Allowed extensions are: {string.Join(", ", AllowedExtensions)}");
        }

        // 2. Validate Size (2MB max)
        if (request.FileStream.Length > 2 * 1024 * 1024)
        {
            return Result<string>.Failure("File size exceeds the 2 MB limit.");
        }

        // 3. Verify Magic Bytes
        var headerBytes = new byte[12];
        var bytesRead = await request.FileStream.ReadAsync(headerBytes, 0, 12, cancellationToken);
        request.FileStream.Position = 0; // Reset position so storage service can read from start

        if (!IsValidImageHeader(headerBytes, bytesRead))
        {
            return Result<string>.Failure("The uploaded file signature is not a valid image format.");
        }

        // 4. Generate Unique Filename
        var uniqueFileName = $"company-{companyId}-{Guid.NewGuid()}{extension}";

        // 5. Upload file (keeps I/O inside Infrastructure via abstraction)
        var relativeUrl = await _fileStorageService.UploadFileAsync(request.FileStream, uniqueFileName, request.ContentType, cancellationToken);

        // 6. Get/Create Settings and rotate logo
        var settings = _context.CompanySettings
            .FirstOrDefault(cs => cs.CompanyId == companyId);

        bool isNew = false;
        if (settings == null)
        {
            settings = new RecruitIQ.Domain.Entities.CompanySettings
            {
                CompanyId = companyId
            };
            isNew = true;
        }

        // Delete old logo if it exists
        if (!string.IsNullOrEmpty(settings.LogoUrl))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(settings.LogoUrl, cancellationToken);
            }
            catch
            {
                // Soft warning, don't crash upload flow if deletion of old file fails
            }
        }

        settings.LogoUrl = relativeUrl;

        if (isNew)
        {
            _context.Add(settings);
        }
        else
        {
            _context.Update(settings);
        }

        // Activity log
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Action = "Company Logo Uploaded",
            EntityName = "CompanySettings",
            EntityId = settings.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(relativeUrl);
    }

    private static bool IsValidImageHeader(byte[] headerBytes, int bytesRead)
    {
        if (bytesRead < 3) return false;

        // JPEG: FF D8 FF
        if (headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            return true;

        if (bytesRead < 4) return false;

        // PNG: 89 50 4E 47
        if (headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47)
            return true;

        if (bytesRead < 12) return false;

        // WEBP: "RIFF" at 0 and "WEBP" at 8
        if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46 &&
            headerBytes[8] == 0x57 && headerBytes[9] == 0x45 && headerBytes[10] == 0x42 && headerBytes[11] == 0x50)
            return true;

        return false;
    }
}
