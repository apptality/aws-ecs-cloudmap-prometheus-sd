using Amazon.ECS.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Contains information about an ECS service
/// </summary>
public class EcsService
{
    public string ServiceArn => Service.ServiceArn;
    public string ServiceName => Service.ServiceName;
    public required string ClusterArn { get; init; }
    public required Service Service { get; init; }
    public ICollection<EcsTask> Tasks { get; set; } = [];

    internal ICollection<ResourceTag> Tags =>
        Service.Tags.Select(t => new ResourceTag
        {
            Key = t.Key, Value = t.Value
        }).ToList();
}