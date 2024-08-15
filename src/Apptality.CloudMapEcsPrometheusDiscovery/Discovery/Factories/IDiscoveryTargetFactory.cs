using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Factories;

public interface IDiscoveryTargetFactory
{
    /// <summary>
    /// Creates a collection of DiscoveryTargets from a DiscoveryResult
    /// </summary>
    /// <param name="discoveryResult">
    /// Discovery result representing the current state of infrastructure
    /// </param>
    /// <returns>
    /// Collection of <see cref="DiscoveryTarget"/>
    /// </returns>
    ICollection<DiscoveryTarget> Create(DiscoveryResult discoveryResult);
}