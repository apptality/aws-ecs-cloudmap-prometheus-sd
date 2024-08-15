namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

/// <summary>
/// Class/type agnostic representation of a resource tag
/// </summary>
public class ResourceTag
{
    public string Key { get; init; } = default!;
    public string Value { get; init; } = default!;

    /// <summary>
    /// Returns true if the tag matches any the selectors provided
    /// </summary>
    /// <param name="selectors">
    /// Multiple selectors to match against
    /// </param>
    public bool MatchesAny(ICollection<ResourceTagSelector> selectors)
    {
        return selectors.Any(selector => Key == selector.Key && (selector.IsValueWildcard || Value == selector.Value));
    }
}