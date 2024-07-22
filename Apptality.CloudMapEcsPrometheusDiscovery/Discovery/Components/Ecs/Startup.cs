namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;

/// <summary>
/// Startup class for adding ECS service discovery components
/// </summary>
public static class Startup
{
    /// <summary>
    /// Adds ECS service discovery components to the application
    /// </summary>
    internal static WebApplicationBuilder AddEcsDiscovery(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IEcsDiscovery, EcsDiscovery>();
        return builder;
    }
}