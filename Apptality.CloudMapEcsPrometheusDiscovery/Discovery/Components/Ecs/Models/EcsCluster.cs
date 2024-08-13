using Amazon.ECS.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Contains information about an ECS cluster
/// </summary>
public class EcsCluster
{
    public string ClusterArn => Cluster.ClusterArn;
    public required Cluster Cluster { get; init; }

    public ICollection<EcsService> Services { get; set; } = [];

    public string ClusterName => Cluster.ClusterName;

    internal ICollection<ResourceTag> Tags =>
        Cluster.Tags.Select(t => new ResourceTag
        {
            Key = t.Key, Value = t.Value
        }).ToList();
}