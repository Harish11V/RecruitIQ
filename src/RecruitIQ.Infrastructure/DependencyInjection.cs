using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Infrastructure.Services;

namespace RecruitIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add options configuration for JwtSettings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Register HTTP context accessor
        services.AddHttpContextAccessor();

        // Register core services
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantService, TenantService>();

        // Configure FileStorageOptions
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
