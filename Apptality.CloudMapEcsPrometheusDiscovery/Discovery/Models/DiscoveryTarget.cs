using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Object representing a target discovered by the service discovery
/// </summary>
public sealed class DiscoveryTarget
{
    /// <summary>
    /// CloudMap Service Name
    /// </summary>
    public string CloudMapServiceName { get; set; } = default!;

    /// <summary>
    /// Identifier of CloudMap Service Instance
    /// </summary>
    public string CloudMapServiceInstanceId { get; set; } = default!;

    /// <summary>
    /// Specifies whether target is a 'Service Connect' or 'Service Discovery' target
    /// </summary>
    public CloudMapServiceType? ServiceType { get; set; } = null;

    /// <summary>
    /// Name of the ECS cluster
    /// </summary>
    public string EcsCluster { get; set; } = default!;

    /// <summary>
    /// Name of the ECS service
    /// </summary>
    public string EcsService { get; set; } = default!;

    /// <summary>
    /// Ecs Task Definition Arn
    /// </summary>
    public string EcsTaskDefinitionArn { get; set; } = default!;

    /// <summary>
    /// Ecs Task Arn
    /// </summary>
    public string EcsTaskArn { get; set; } = default!;

    /// <summary>
    /// IP Address of the ECS task
    /// </summary>
    public string IpAddress { get; set; } = default!;

    /// <summary>
    /// Each task can have multiple ports exposed, with each port having a different scrape configuration
    /// </summary>
    /// <remarks>
    /// Typical use case is application running on port 8080, and ecs-exporter running on port 9779.
    /// Thus, having at least two scrape configurations for each task.
    /// </remarks>
    public List<ScrapeConfiguration> ScrapeConfigurations { get; set; } = [];

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