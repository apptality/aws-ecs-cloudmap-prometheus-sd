using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a service in namespace
/// </summary>
public class CloudMapService
{
    public required string NamespaceId { get; init; }
    public CloudMapNamespace CloudMapNamespace { get; set; }
    public required ServiceSummary ServiceSummary { get; init; }
    public ResourceTag[] Tags { get; set; } = [];
    public ICollection<CloudMapServiceInstance> InstanceSummaries { get; set; } = [];
    public CloudMapServiceType? ServiceType { get; set; }

    /// <summary>
    /// Returns all instances that match the provided ip address
    /// </summary>
    /// <param name="ip">IP address to match on</param>
    /// <returns>
    /// All CloudMap service instances that match the provided IP address
    /// </returns>
    public ICollection<CloudMapServiceInstance> FindInstanceByIp(string ip)
    {
        return InstanceSummaries.Where(instance => instance.IpAddress() == ip).ToList();
    }
}