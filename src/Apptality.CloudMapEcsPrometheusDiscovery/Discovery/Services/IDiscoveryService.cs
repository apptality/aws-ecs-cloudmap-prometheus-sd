using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

/// <summary>
/// Interface for service discovery
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Discovers ECS clusters and CloudMap namespaces and return the result
    /// </summary>
    Task<DiscoveryResult> Discover();
}