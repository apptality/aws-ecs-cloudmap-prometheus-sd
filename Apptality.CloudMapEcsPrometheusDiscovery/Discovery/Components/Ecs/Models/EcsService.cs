using Amazon.ECS.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Contains information about an ECS service
/// </summary>
public class EcsService
{
    public string ServiceArn => Service.ServiceArn;
    public required string ClusterArn { get; init; }
    public required Service Service { get; init; }
    public ICollection<EcsTask> Tasks { get; set; } = [];
}