namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Represent object containing information about metadata labels
/// </summary>
public class DiscoveryLabel
{
    /// <summary>
    /// Label name
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Label value
    /// </summary>
    public required string Value { get; init; }
    /// <summary>
    /// Label priority
    /// </summary>
    public required DiscoveryLabelPriority Priority { get; init; }
}