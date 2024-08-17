using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

public interface ICloudMapDiscoveryService
{
    /// <summary>
    /// Discovers information about CloudMap namespaces, services and instances
    /// </summary>
    /// <param name="extraCloudMapNamespaces">
    /// Supply additional CloudMap namespaces to discover,
    /// excluding the ones already configured in the settings
    /// </param>
    Task<ICollection<CloudMapNamespace>> DiscoverCloudMapResources(
        ICollection<string>? extraCloudMapNamespaces = null
    );
}