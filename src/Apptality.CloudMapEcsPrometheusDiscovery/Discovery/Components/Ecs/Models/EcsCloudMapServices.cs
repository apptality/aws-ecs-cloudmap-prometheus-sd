namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

/// <summary>
/// Holds information about ECS services that are associated with a Cloud Map namespace.
/// </summary>
/// <param name="CloudMapNamespaceArn">CloudMap namespace ARN</param>
/// <param name="ServiceArns">ECS Services ARNs</param>
public record EcsCloudMapServices(string CloudMapNamespaceArn, string[] ServiceArns);