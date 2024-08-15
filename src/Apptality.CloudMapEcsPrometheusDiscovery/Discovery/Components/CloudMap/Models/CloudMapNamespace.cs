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
    public ICollection<CloudMapService> ServiceSummaries { get; private set; } = [];
    public ResourceTag[] Tags { get; set; } = [];

    // Set the service summaries for the namespace
    public void SetServiceSummaries(List<CloudMapService> relatedServiceSummaries)
    {
        ServiceSummaries = relatedServiceSummaries;
        foreach (var serviceSummary in ServiceSummaries)
        {
            serviceSummary.CloudMapNamespace = this;
        }
    }
}