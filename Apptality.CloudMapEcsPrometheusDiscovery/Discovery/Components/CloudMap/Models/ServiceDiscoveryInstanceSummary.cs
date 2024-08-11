using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a service instance
/// </summary>
public class ServiceDiscoveryInstanceSummary
{
    public required string ServiceId { get; init; }
    public required InstanceSummary InstanceSummary { get; init; }
}