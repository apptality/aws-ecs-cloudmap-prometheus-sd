namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Enum is used to associate labels with precedence priority
/// </summary>
public enum DiscoveryLabelPriority
{
    /// <summary>
    /// Relabel labels are of the highest priority and will take precedence over other labels.
    /// This is because relabeling is done after all labels have been constructed, but right before
    /// the target is built. This gives developer full control over the target labels, and how they
    /// should be constructed.
    /// </summary>
    Relabel = 0,

    /// <summary>
    /// System labels are of the second-highest priority, intended for internal use,
    /// and will take precedence over other labels.
    /// </summary>
    System = 1,

    /// <summary>
    /// Metadata labels are of the third-highest priority and will take precedence over other labels.
    /// These used by Prometheus to group targets.
    /// </summary>
    Metadata = 2,

    /// <summary>
    /// Labels inferred from tags resolved from the ECS Tasks are of the highest priority,
    /// and will take precedence over other tags resolved from parent service, CloudMap service,
    /// or any other priority with higher value.
    /// </summary>
    EcsTask = 10,

    EcsService = 20,

    CloudMapService = 30,

    CloudMapNamespace = 40,

    /// <summary>
    /// Labels supplied via `ExtraPrometheusLabels` parameter are of the lowest priority, and will
    /// take the lowest precedence.
    /// </summary>
    ExtraLabels = 50,
}