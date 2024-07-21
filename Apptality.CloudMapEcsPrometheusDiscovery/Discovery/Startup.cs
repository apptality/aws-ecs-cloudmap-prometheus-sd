using Amazon.ECS;
using Amazon.ServiceDiscovery;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery;

/// <summary>
/// Startup class for adding service discovery
/// </summary>
public static class Startup
{
    /// <summary>
    /// Adds discovery configurations to the application
    /// </summary>
    private static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder)
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

    /// <summary>
    /// Adds AWS services to the application
    /// </summary>
    /// <see cref="https://aws.amazon.com/blogs/developer/configuring-aws-sdk-with-net-core/"/>
    private static WebApplicationBuilder AddAws(this WebApplicationBuilder builder)
    {
        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonECS>();
        builder.Services.AddAWSService<IAmazonServiceDiscovery>();
        return builder;
    }

    /// <summary>
    /// Adds all infrastructure configurations to the application
    /// </summary>
    internal static WebApplicationBuilder AddDiscovery(this WebApplicationBuilder builder)
    {
        return builder
            .AddConfiguration()
            .AddAws()
            .AddEcsDiscovery();
    }
}