namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// This enum is used to associate labels with precedence priority
/// </summary>
public enum DiscoveryLabelPriority
{
    /// <summary>
    /// Labels inferred from tags resolved from the AWS CloudMap Service are of the highest priority,
    /// and will take precedence over other tags resolved from parent services/"container" resources.
    /// </summary>
    CloudMapService = 0,

    CloudMapNamespace = 10,

    EcsTask = 20,

    /// <summary>
    /// Labels inferred from tags resolved from ECS services are of the lowest priority, and will
    /// take the lowest precedence.
    /// </summary>
    EcsService = 30,
}