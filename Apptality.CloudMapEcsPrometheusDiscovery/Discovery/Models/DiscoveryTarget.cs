namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Object representing a target discovered by the service discovery
/// </summary>
public class DiscoveryTarget
{
    /// <summary>
    /// Identifier of Service Discovery instance
    /// </summary>
    public required string ServiceDiscoveryInstanceId { get; set; }

    /// <summary>
    /// Helps to determine whether target is a service connect or service discovery target
    /// </summary>
    public required DiscoveryTargetServiceType ServiceType { get; set; }

    /// <summary>
    /// IP Address of the ECS task
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Each task can have multiple ports exposed, with each port having a different scrape configuration
    /// </summary>
    /// <remarks>
    /// Typical use case is application running on port 8080, and ecs-exporter running on port 9779.
    /// Thus, having at least two scrape configurations for each task.
    /// </remarks>
    public required List<ScrapeConfiguration> ScrapeConfigurations { get; set; }

    /// <summary>
    /// CloudMap namespace name
    /// </summary>
    public required string CloudMapNamespace { get; set; }

    /// <summary>
    /// Name of the ECS cluster
    /// </summary>
    public required string EcsCluster { get; set; }

    /// <summary>
    /// Name of the ECS service
    /// </summary>
    public required string EcsService { get; set; }

    /// <summary>
    /// Name of the ECS task definition
    /// </summary>
    public required string EcsTaskDefinition { get; set; }

    /// <summary>
    /// Ecs Task Id
    /// </summary>
    public required string EcsTaskId { get; set; }

    /// <summary>
    /// Additional metadata labels resolved for given target
    /// </summary>
    public List<DiscoveryLabel> Labels { get; set; } = [];
}

public sealed class ScrapeConfiguration
{
    public required ushort Port { get; set; }
    public required string MetricsPath { get; set; } = "/metrics";
}