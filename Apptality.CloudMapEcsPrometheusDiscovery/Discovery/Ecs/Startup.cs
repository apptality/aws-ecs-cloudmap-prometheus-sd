namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Ecs;

/// <summary>
/// Startup class for adding ECS service discovery components
/// </summary>
public static class Startup
{
    /// <summary>
    /// Adds all infrastructure configurations to the application
    /// </summary>
    internal static WebApplicationBuilder AddEcsDiscovery(this WebApplicationBuilder builder)
    {
        return builder;
    }
}