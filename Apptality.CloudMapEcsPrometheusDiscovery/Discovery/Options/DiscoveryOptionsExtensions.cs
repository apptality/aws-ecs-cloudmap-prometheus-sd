using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Contains extension methods for working with Options
/// </summary>
public static class DiscoveryOptionsExtensions
{
    /// <summary>
    /// Parses "EcsClusters" parameter from the DiscoveryOptions and returns an array of ECS clusters
    /// </summary>
    public static string[] GetEcsClustersNames(this DiscoveryOptions discoveryOptions)
    {
        // Resolve clusters names from configuration
        return CommaSeparatedStringToArray(discoveryOptions.EcsClusters);
    }

    /// <summary>
    /// Parses "CloudMapNamespaces" parameter from the DiscoveryOptions and returns an array of CloudMap namespaces
    /// </summary>
    public static string[] GetCloudMapNamespaceNames(this DiscoveryOptions discoveryOptions)
    {
        // Resolve namespace names from configuration
        return CommaSeparatedStringToArray(discoveryOptions.CloudMapNamespaces);
    }

    /// <summary>
    /// Parses "EcsServiceSelectorTags" parameter from the DiscoveryOptions and returns an array of tags to match services on
    /// </summary>
    public static ResourceTagSelector[] GetEcsServiceSelectorTags(this DiscoveryOptions discoveryOptions)
    {
        return CommaSeparatedTagsStringToArray(discoveryOptions.EcsServiceSelectorTags);
    }

    /// <summary>
    /// Parses "CloudMapServiceSelectorTags" parameter from the DiscoveryOptions and returns an array of tags to match services on
    /// </summary>
    public static ResourceTagSelector[] GetCloudMapServiceSelectorTags(this DiscoveryOptions discoveryOptions)
    {
        return CommaSeparatedTagsStringToArray(discoveryOptions.CloudMapServiceSelectorTags);
    }

    /// <summary>
    /// Internal helper method to convert a semicolon separated string of key-value pairs to an array
    /// </summary>
    /// <param name="value">
    /// String containing key-value pairs separated by semicolon
    /// </param>
    private static ResourceTagSelector[] CommaSeparatedTagsStringToArray(string value)
    {
        var tagKeyValuePairs = CommaSeparatedStringToArray(value);
        return tagKeyValuePairs.Select(tag =>
        {
            var tagParts = tag.Split('=');
            return new ResourceTagSelector {Key = tagParts[0], Value = tagParts.Length > 1 ? tagParts[1] : null};
        }).ToArray();
    }

    /// <summary>
    /// Internal helper method to convert a semicolon separated string to an array
    /// </summary>
    /// <param name="value">String to convert</param>
    /// <returns>
    /// Array of strings
    /// </returns>
    private static string[] CommaSeparatedStringToArray(string value)
    {
        return value.Split(';').Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
    }
}