using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Task = Amazon.ECS.Model.Task;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Contains information about an ECS task
/// </summary>
public class EcsTask
{
    public string TaskArn => Task.TaskArn;
    public required string ClusterArn { get; init; }
    public required string ServiceArn { get; init; }
    public required Task Task { get; init; }

    public bool IsRunning()
    {
        return Task is
        {
            DesiredStatus: "RUNNING", LastStatus: "RUNNING"
        } || this.Task.LastStatus == "PENDING";
    }

    public string IpAddress()
    {
        return Task.Containers.FirstOrDefault()?.NetworkInterfaces.FirstOrDefault()?.PrivateIpv4Address ?? string.Empty;
    }

    internal ICollection<ResourceTag> Tags =>
        Task.Tags.Select(t => new ResourceTag
        {
            Key = t.Key, Value = t.Value
        }).ToList();
}