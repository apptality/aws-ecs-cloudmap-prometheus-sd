using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a service in namespace
/// </summary>
public class ServiceDiscoveryServiceSummary
{
    public required string NamespaceId { get; init; }
    public required ServiceSummary ServiceSummary { get; init; }
    public ResourceTag[] Tags { get; set; } = [];
    public ICollection<InstanceSummary> InstanceSummaries { get; set; } = [];
}