using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Represents a summary of a service instance
/// </summary>
public class CloudMapServiceInstance
{
    public string Id => InstanceSummary.Id;
    public required string ServiceId { get; init; }
    public required InstanceSummary InstanceSummary { get; init; }

    /// <summary>
    /// Returns the IP address of the instance
    /// </summary>
    public string IpAddress()
    {
        return InstanceSummary.Attributes["AWS_INSTANCE_IPV4"] ?? string.Empty;
    }

    /// <summary>
    /// Returns the ECS cluster name of the instance.
    /// </summary>
    /// <remarks>
    /// Only applicable to ServiceDiscovery instances that are part of an ECS service.
    /// </remarks>
    public string EcsClusterName()
    {
        return InstanceSummary.Attributes.TryGetValue("ECS_CLUSTER_NAME", out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Returns the ECS service name of the instance.
    /// </summary>
    /// <remarks>
    /// Only applicable to ServiceDiscovery instances that are part of an ECS service.
    /// </remarks>
    public string EcsServiceName()
    {
        return InstanceSummary.Attributes.TryGetValue("ECS_SERVICE_NAME", out var value) ? value : string.Empty;
    }
}