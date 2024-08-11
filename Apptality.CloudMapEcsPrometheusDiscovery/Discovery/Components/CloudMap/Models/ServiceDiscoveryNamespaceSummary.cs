using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a namespace
/// </summary>
public class ServiceDiscoveryNamespaceSummary
{
    public string Arn => NamespaceSummary.Arn;
    public required NamespaceSummary NamespaceSummary { get; init; }
    public ICollection<ServiceDiscoveryServiceSummary> ServiceSummaries { get; set; } = [];
    public ResourceTag[] Tags { get; set; } = [];
}