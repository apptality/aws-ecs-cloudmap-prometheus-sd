namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Startup class for adding discovery settings (options) to the application
/// </summary>
public static class Startup
{
    internal static WebApplicationBuilder AddOptions(this WebApplicationBuilder builder)
    {
        builder.Services
            // Add discovery options to the configuration
            .AddOptions<DiscoveryOptions>()
            .BindConfiguration(nameof(DiscoveryOptions))
            // Ensure configurations are validated on start
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }
}