using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Extensions;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
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
    /// Returns list of extra Prometheus labels from the configuration
    /// </summary>
    public static ICollection<DiscoveryLabel> GetExtraPrometheusLabels(this DiscoveryOptions discoveryOptions)
    {
        var extraLabels = new List<DiscoveryLabel>();
        var extraLabelsSource = CommaSeparatedStringToArray(discoveryOptions.ExtraPrometheusLabels);
        foreach (var extraLabel in extraLabelsSource)
        {
            var (key, value) = ParseKeyValuePair(extraLabel);
            if (string.IsNullOrWhiteSpace(value)) continue;
            extraLabels.Add(new DiscoveryLabel
            {
                Name = key, Value = value,
                Priority = DiscoveryLabelPriority.ExtraLabels
            });
        }

        return extraLabels;
    }

    /// <summary>
    /// Returns list of tag key filters that can be used
    /// to limit the tags that are included in the service discovery results
    /// </summary>
    public static ICollection<string> GetEcsTaskTagFilters(this DiscoveryOptions discoveryOptions)
    {
        return GetTagKeyFilters(discoveryOptions, x => x.EcsTaskTags);
    }

    /// <summary>
    /// Returns list of tag key filters that can be used
    /// to limit the tags that are included in the service discovery results
    /// </summary>
    public static ICollection<string> GetEcsServiceTagFilters(this DiscoveryOptions discoveryOptions)
    {
        return GetTagKeyFilters(discoveryOptions, x => x.EcsServiceTags);
    }

    /// <summary>
    /// Returns list of tag key filters that can be used
    /// to limit the tags that are included in the service discovery results
    /// </summary>
    public static ICollection<string> GetCloudMapServiceTagFilters(this DiscoveryOptions discoveryOptions)
    {
        return GetTagKeyFilters(discoveryOptions, x => x.CloudMapServiceTags);
    }

    /// <summary>
    /// Returns list of tag key filters that can be used
    /// to limit the tags that are included in the service discovery results
    /// </summary>
    public static ICollection<string> GetCloudMapNamespaceTagFilters(this DiscoveryOptions discoveryOptions)
    {
        return GetTagKeyFilters(discoveryOptions, x => x.CloudMapNamespaceTags);
    }

    /// <summary>
    /// Based on the configuration, returns a list of tag key filters
    /// </summary>
    /// <returns>
    /// Collection of tag "Key" property value filters that can be used
    /// as 'pattern' in Regex.IsMatch method (Regex.IsMatch(value, pattern)).
    /// </returns>
    private static ICollection<string> GetTagKeyFilters(
        this DiscoveryOptions discoveryOptions,
        Func<DiscoveryOptions, string> selector
    )
    {
        var tagKeyFilters = new List<string>();
        var tagKeyFiltersSource = CommaSeparatedStringToArray(selector(discoveryOptions));
        foreach (var tagKeyFilter in tagKeyFiltersSource)
        {
            if (string.IsNullOrWhiteSpace(tagKeyFilter)) continue;
            tagKeyFilters.Add(tagKeyFilter.ConvertToRegexString());
        }

        // Always include 'MetricsPathPortTagPrefix' tag key filter,
        // as these are used to identify the path/port pairs of the metrics endpoint
        tagKeyFilters.Add($"{discoveryOptions.MetricsPathPortTagPrefix}*".ConvertToRegexString());

        return tagKeyFilters;
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
            var (key, val) = ParseKeyValuePair(tag);
            return new ResourceTagSelector {Key = key, Value = val};
        }).ToArray();
    }

    /// <summary>
    /// Parses key-value pairs from a string and returns a tuple
    /// </summary>
    /// <param name="value">
    /// Value to parse. Key and value are separated by an equal sign.
    /// When equal sign should be treated as part of the value,
    /// it must be declared as "%=" in the source string.
    /// </param>
    /// <returns>
    /// Tuple containing key and value
    /// </returns>
    private static Tuple<string, string?> ParseKeyValuePair(string value)
    {
        const string equalSignSourceToken = "%=";
        const string equalSignReplacementToken = "<equal>";

        value = value.Replace(equalSignSourceToken, equalSignReplacementToken);
        var parts = value.Split('=');
        var key = parts[0];
        var val = parts.Length > 1 ? parts[1] : null;
        if (val != null)
        {
            val = val.Replace(equalSignReplacementToken, "=");
        }

        return new Tuple<string, string?>(key, val);
    }

    /// <summary>
    /// Internal helper method to convert a semicolon separated string to an array
    /// </summary>
    /// <param name="value">String to convert</param>
    /// <returns>
    /// Array of strings
    /// </returns>
    /// <remarks>
    /// Any semicolons that should be treated as part of the string
    /// must be declared as "%;" in the source string.
    /// </remarks>
    private static string[] CommaSeparatedStringToArray(string value)
    {
        const string semicolonSourceToken = "%;";
        const string semicolonReplacementToken = "<semicolon>";
        value = value.Replace(semicolonSourceToken, semicolonReplacementToken);

        return value.Split(';')
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Replace(semicolonReplacementToken, ";"))
            .ToArray();
    }
}