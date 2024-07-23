namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Enum is used to associate labels with precedence priority
/// </summary>
public enum DiscoveryLabelPriority
{
    /// <summary>
    /// Labels inferred from tags resolved from the AWS CloudMap Service are of the highest priority,
    /// and will take precedence over other tags resolved from parent services/"container" resources.
    /// </summary>
    CloudMapService = 10,

    CloudMapNamespace = 20,

    EcsTask = 30,

    EcsService = 40,

    /// <summary>
    /// Labels supplied via `ExtraPrometheusLabels` parameter are of the lowest priority, and will
    /// take the lowest precedence.
    /// </summary>
    ExtraLabels = 50,
}