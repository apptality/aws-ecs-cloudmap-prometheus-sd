using Amazon.ECS;
using Amazon.ServiceDiscovery;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery;

/// <summary>
/// Startup class for adding service discovery
/// </summary>
public static class Startup
{
    /// <summary>
    /// Adds AWS services to the application
    /// </summary>
    /// <see cref="https://aws.amazon.com/blogs/developer/configuring-aws-sdk-with-net-core/"/>
    private static WebApplicationBuilder AddAwsServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonECS>();
        builder.Services.AddAWSService<IAmazonServiceDiscovery>();
        return builder;
    }

    /// <summary>
    /// Adds Discovery BLL services to the application
    /// </summary>
    private static WebApplicationBuilder AddDiscoveryServices(this WebApplicationBuilder builder)
    {
        // original service
        builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
        // cached decorator
        builder.Services.Decorate<IDiscoveryService, CachedDiscoveryService>();
        return builder;
    }

    /// <summary>
    /// Adds services and configurations that support the business logic of the application
    /// </summary>
    internal static WebApplicationBuilder AddDiscovery(this WebApplicationBuilder builder)
    {
        return builder
            .AddOptions()
            .AddAwsServices()
            .AddEcsDiscovery()
            .AddCloudMapDiscovery()
            .AddDiscoveryServices();
    }
}