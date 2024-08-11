namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Represents an object that holds information about running ECS tasks for given cluster and service
/// </summary>
public record EcsServiceTasks(string ClusterArn, string ServiceArn, string[] RunningTaskArns);