using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Represents an object designed to hold a shared state as ECS
/// discovery is progressing through applying business logic
/// </summary>
public class DiscoveryResult
{
    /// <summary>
    /// Options for the discovery service
    /// </summary>
    public required DiscoveryOptions DiscoveryOptions { get; set; }

    /// <summary>
    /// Target that haven been discovered
    /// </summary>
    public List<DiscoveryTarget> DiscoveryTargets { get; set; } = [];
}