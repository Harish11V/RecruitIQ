using System;
using Microsoft.AspNetCore.Http;
using RecruitIQ.Application.Common.Interfaces;

namespace RecruitIQ.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Guid.Empty;
            }

            // 1. Try to get from HttpContext.Items (populated by TenantMiddleware)
            if (httpContext.Items.TryGetValue("CompanyId", out var companyIdObj) && companyIdObj is Guid companyId)
            {
                return companyId;
            }

            // 2. Try to get from claims if authenticated
            var companyIdClaim = httpContext.User?.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var claimCompanyId))
            {
                return claimCompanyId;
            }

            return Guid.Empty;
        }
    }
}
