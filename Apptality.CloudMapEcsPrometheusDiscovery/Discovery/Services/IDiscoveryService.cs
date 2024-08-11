using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

/// <summary>
/// Interface for service discovery
/// </summary>
public interface IDiscoveryService
{
    Task<DiscoveryResult> Discover();
}