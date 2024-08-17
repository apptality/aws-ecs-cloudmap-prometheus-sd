using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

public interface IEcsDiscoveryService
{
    /// <summary>
    /// Discovers information about ECS clusters, services and tasks
    /// </summary>
    /// <param name="extraEcsClusters">
    /// Supply additional ECS clusters to discover,
    /// outside the ones already configured in the settings
    /// </param>
    Task<ICollection<EcsCluster>> DiscoverEcsResources(
        ICollection<string>? extraEcsClusters = null
    );
}