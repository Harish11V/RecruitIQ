using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.CompanySettings.DeleteCompanyLogo;

public class DeleteCompanyLogoCommandHandler : IRequestHandler<DeleteCompanyLogoCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public DeleteCompanyLogoCommandHandler(
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

    public async Task<Result> Handle(DeleteCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        var settings = _context.CompanySettings
            .FirstOrDefault(cs => cs.CompanyId == companyId);

        if (settings == null || string.IsNullOrEmpty(settings.LogoUrl))
        {
            return Result.Success();
        }

        // Delete file from storage
        try
        {
            await _fileStorageService.DeleteFileAsync(settings.LogoUrl, cancellationToken);
        }
        catch
        {
            // Fail silently or log, but proceed to remove database reference
        }

        settings.LogoUrl = null;
        _context.Update(settings);

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
            Action = "Company Logo Deleted",
            EntityName = "CompanySettings",
            EntityId = settings.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
