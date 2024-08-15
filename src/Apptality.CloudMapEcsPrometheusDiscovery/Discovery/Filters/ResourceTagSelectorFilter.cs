namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

public class ResourceTagSelectorFilter(ResourceTagSelector[] tagSelectors)
{
    // Holds the tag selectors to match against
    private int ExpectedNumberOfMatches => tagSelectors.Length;

    /// <summary>
    /// Matches the provided tags against the tag selectors
    /// </summary>
    public bool Matches(ICollection<ResourceTag> tags)
    {
        // If no tag selectors provided, return true.
        // This adheres to the application use case
        // when no filters are supplied.
        if (tagSelectors.Length < 1) return true;
        return tags.Count(tag => tag.MatchesAny(tagSelectors)) == ExpectedNumberOfMatches;
    }
}