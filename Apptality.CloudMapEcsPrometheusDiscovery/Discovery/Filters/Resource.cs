namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

/// <summary>
/// Holds information about
/// given AWS resource with it's ARN, and it's tags.
/// </summary>
public class Resource
{
    public required string Arn { get; set; }

    public ICollection<ResourceTag> Tags { get; set; } = [];

    /// <summary>
    /// Generates a ResourceTags object from a given object
    /// </summary>
    /// <param name="obj">Object to generate resource tags from</param>
    /// <param name="arnSelector">Function to select ARN from the object</param>
    /// <param name="tagSelector">Function to select tags from the object</param>
    public static Resource FromObject<T>(
        T obj,
        Func<T, string> arnSelector,
        Func<T, ICollection<ResourceTag>> tagSelector
    )
    {
        return new Resource
        {
            Arn = arnSelector(obj),
            Tags = tagSelector(obj)
        };
    }
}