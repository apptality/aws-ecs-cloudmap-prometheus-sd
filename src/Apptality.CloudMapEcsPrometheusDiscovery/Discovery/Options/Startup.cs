using Microsoft.Extensions.Options;

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
            .BindConfiguration(nameof(DiscoveryOptions));

        builder.Services
            // Add validators for the discovery options
            .AddSingleton<IValidateOptions<DiscoveryOptions>, DiscoveryOptionsValidator>()
            .AddSingleton<IValidateOptions<DiscoveryOptions>, DiscoveryOptionsCustomValidator>();

        return builder;
    }
}