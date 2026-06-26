using Microsoft.AspNetCore.Http;
using RecruitIQ.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecruitIQ.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRecruitIQDbContext dbContext)
    {
        string? subdomain = null;

        // 1. Check headers
        if (context.Request.Headers.TryGetValue("X-Tenant", out var tenantHeader))
        {
            subdomain = tenantHeader.ToString();
        }
        else if (context.Request.Headers.TryGetValue("X-Subdomain", out var subdomainHeader))
        {
            subdomain = subdomainHeader.ToString();
        }
        else
        {
            // 2. Parse host (e.g., tenant.localhost:5000 or tenant.recruitiq.com)
            var host = context.Request.Host.Host;
            var segments = host.Split('.');

            if (segments.Length > 1 && !segments[0].Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                if (segments.Length == 2 && segments[1].Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    subdomain = segments[0];
                }
                else if (segments.Length > 2)
                {
                    subdomain = segments[0];
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            subdomain = subdomain.Trim().ToLower();

            var company = await dbContext.Companies
                .Where(c => c.Subdomain == subdomain && c.IsActive && !c.IsDeleted)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync();

            if (company != null)
            {
                context.Items["CompanyId"] = company.Id;
            }
        }

        await _next(context);
    }
}
