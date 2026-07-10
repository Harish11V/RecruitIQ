using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;

namespace RecruitIQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Register Lifecycle Service
        services.AddScoped<Common.Interfaces.ICandidateLifecycleService, Services.CandidateLifecycleService>();

        return services;
    }
}
