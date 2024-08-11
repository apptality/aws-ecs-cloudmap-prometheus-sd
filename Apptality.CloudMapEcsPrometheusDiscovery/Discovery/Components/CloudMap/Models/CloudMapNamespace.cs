using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a namespace
/// </summary>
public class CloudMapNamespace
{
    public string Arn => NamespaceSummary.Arn;
    public string Id => NamespaceSummary.Id;
    public required NamespaceSummary NamespaceSummary { get; init; }
    public ICollection<CloudMapService> ServiceSummaries { get; set; } = [];
    public ResourceTag[] Tags { get; set; } = [];
}