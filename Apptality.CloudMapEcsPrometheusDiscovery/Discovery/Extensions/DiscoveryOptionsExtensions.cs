using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Extensions;

/// <summary>
/// Contains extension methods for working with Options
/// </summary>
public static class DiscoveryOptionsExtensions
{
    /// <summary>
    /// Parses "EcsClusters" parameter from the DiscoveryOptions and returns an array of ECS clusters
    /// </summary>
    public static string[] GetEcsClusters(this DiscoveryOptions discoveryOptions)
    {
        return discoveryOptions.EcsClusters.Split(';').Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
    }

}