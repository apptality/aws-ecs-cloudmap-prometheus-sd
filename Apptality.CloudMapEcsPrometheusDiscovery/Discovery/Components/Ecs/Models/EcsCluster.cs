using Amazon.ECS.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Contains information about an ECS cluster
/// </summary>
public class EcsCluster
{
    public string ClusterArn => Cluster.ClusterArn;
    public required Cluster Cluster { get; init; }

    public ICollection<EcsService> Services { get; set; } = [];
}