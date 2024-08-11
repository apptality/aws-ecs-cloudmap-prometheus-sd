using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Object representing a target discovered by the service discovery
/// </summary>
public sealed class DiscoveryTarget
{
    /// <summary>
    /// Identifier of Service Discovery instance
    /// </summary>
    public string ServiceDiscoveryInstanceId { get; set; }

    /// <summary>
    /// Determines whether target is a service connect or service discovery target
    /// </summary>
    public CloudMapServiceType? ServiceType { get; set; } = null;

    /// <summary>
    /// IP Address of the ECS task
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// Each task can have multiple ports exposed, with each port having a different scrape configuration
    /// </summary>
    /// <remarks>
    /// Typical use case is application running on port 8080, and ecs-exporter running on port 9779.
    /// Thus, having at least two scrape configurations for each task.
    /// </remarks>
    public List<ScrapeConfiguration> ScrapeConfigurations { get; set; } = [];

    /// <summary>
    /// CloudMap namespace name
    /// </summary>
    public string CloudMapNamespace { get; set; }

    /// <summary>
    /// Name of the ECS cluster
    /// </summary>
    public string EcsCluster { get; set; }

    /// <summary>
    /// Name of the ECS service
    /// </summary>
    public string EcsService { get; set; }

    /// <summary>
    /// Name of the ECS task definition
    /// </summary>
    public string EcsTaskDefinition { get; set; }

    /// <summary>
    /// Ecs Task Id
    /// </summary>
    public string EcsTaskId { get; set; }

    /// <summary>
    /// Additional metadata labels resolved for given target
    /// </summary>
    public List<DiscoveryLabel> Labels { get; set; } = [];
}

/// <summary>
/// Model representing a scrape configuration for a target,
/// as ecs service can have multiple ports exposed, each with different scrape configuration
/// </summary>
public sealed class ScrapeConfiguration
{
    public ushort Port { get; set; }
    public string MetricsPath { get; set; } = "/metrics";
}