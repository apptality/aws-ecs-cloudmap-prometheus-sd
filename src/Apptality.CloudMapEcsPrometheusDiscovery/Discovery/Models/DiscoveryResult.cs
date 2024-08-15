using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
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
    /// Ecs clusters discovered
    /// </summary>
    public ICollection<EcsCluster> EcsClusters { get; set; } = [];

    /// <summary>
    /// CloudMap namespaces discovered
    /// </summary>
    public ICollection<CloudMapNamespace> CloudMapNamespaces { get; set; } = [];
}