namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// The Model represents a key-value pair for a resource tag,
/// where value is optional. Used for filtering.
/// </summary>
public sealed class ResourceTagSelector
{
    public string Key { get; init; } = default!;
    public string? Value { get; init; }

    public bool HasValue => Value is not null;

    public bool IsValueWildcard => Value == "*";
}