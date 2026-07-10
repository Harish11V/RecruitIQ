using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.CompanySettings.UpdateCompanySettings;

public class UpdateCompanySettingsCommandHandler : IRequestHandler<UpdateCompanySettingsCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanySettingsCommandHandler(
        IRecruitIQDbContext context, 
        ITenantService tenantService, 
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

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

        settings.Theme = request.Theme;
        settings.Timezone = request.Timezone;
        settings.DefaultInterviewDuration = request.DefaultInterviewDuration;
        settings.AllowedEmailDomain = request.AllowedEmailDomain;

        if (isNew)
        {
            _context.Add(settings);
        }
        else
        {
            _context.Update(settings);
            // Apply concurrency check original value
            _context.SetOriginalRowVersion(settings, request.RowVersion);
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
            Action = "Company Settings Updated",
            EntityName = "CompanySettings",
            EntityId = settings.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result.Failure("A concurrency conflict occurred. The settings have been modified by another administrator.");
        }

        return Result.Success();
    }
}
