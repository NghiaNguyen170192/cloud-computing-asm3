using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Application.Behaviors;
using System.Reflection;

namespace NetCore.Donation.Application.Extensions;

/// <summary>
/// Extension methods for configuring CQRS dispatcher and handlers.
/// </summary>
public static class DispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Adds the CQRS dispatcher and scans for handlers in the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for handlers. If none provided, scans the calling assembly.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDispatcher(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        // Register MediatR IMediator and handlers/behaviors
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    /// <summary>
    /// Adds a custom pipeline behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TBehavior));
        return services;
    }
}