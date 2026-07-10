using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.CompanySettings.GetCompanySettings;

public class GetCompanySettingsQueryHandler : IRequestHandler<GetCompanySettingsQuery, Result<CompanySettingsResponse>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCompanySettingsQueryHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result<CompanySettingsResponse>> Handle(GetCompanySettingsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        var settings = _context.CompanySettings
            .FirstOrDefault(cs => cs.CompanyId == companyId);

        if (settings == null)
        {
            settings = new RecruitIQ.Domain.Entities.CompanySettings
            {
                CompanyId = companyId,
                Theme = "Light",
                Timezone = "UTC",
                DefaultInterviewDuration = 30
            };

            _context.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var response = new CompanySettingsResponse(
            settings.CompanyId,
            settings.Theme,
            settings.LogoUrl,
            settings.Timezone,
            settings.DefaultInterviewDuration,
            settings.AllowedEmailDomain,
            settings.RowVersion);

        return Result<CompanySettingsResponse>.Success(response);
    }
}
