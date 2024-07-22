namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;

public static class Startup
{
    /// <summary>
    /// Adds ServiceDiscovery service discovery components to the application
    /// </summary>
    internal static WebApplicationBuilder AddCloudMapDiscovery(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IServiceDiscovery, ServiceDiscovery>();
        return builder;
    }
}