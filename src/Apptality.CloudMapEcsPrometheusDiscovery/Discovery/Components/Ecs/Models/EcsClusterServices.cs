namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Holds information about ECS cluster services
/// </summary>
/// <param name="ClusterArn">ECS cluster ARN</param>
/// <param name="ServiceArns">ECS Servcies ARNs</param>
public record EcsClusterServices(string ClusterArn, string[] ServiceArns);