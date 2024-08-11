using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a service in namespace
/// </summary>
public class CloudMapService
{
    public required string NamespaceId { get; init; }
    public required ServiceSummary ServiceSummary { get; init; }
    public ResourceTag[] Tags { get; set; } = [];
    public ICollection<CloudMapServiceInstance> InstanceSummaries { get; set; } = [];
}