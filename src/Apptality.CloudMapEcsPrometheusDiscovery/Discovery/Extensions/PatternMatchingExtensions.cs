using System.Text.RegularExpressions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Extensions;

/// <summary>
/// Family of helper methods to match values against simple glob patterns
/// </summary>
public static class PatternMatchingExtensions
{
    /// <summary>
    /// Function to match values against patterns
    /// </summary>
    /// <param name="value">
    /// Value to match against patterns
    /// </param>
    /// <param name="patterns">
    /// Collection of patterns to match against
    /// </param>
    /// <returns>
    /// True if the value matches any of the patterns, false otherwise
    /// </returns>
    public static bool Match(this string value, ICollection<string> patterns)
    {
        return patterns
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Any(pattern => Regex.IsMatch(value, pattern));
    }

    /// <summary>
    /// Helper method to convert a pattern to a regex
    /// </summary>
    /// <param name="pattern">
    /// Pattern to convert to regex. Supports '*' and '?'
    /// </param>
    /// <returns>
    /// Regex pattern
    /// </returns>
    public static string ConvertToRegexString(this string pattern)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return "^" + Regex.Escape(pattern) + "$";
        }

        var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return regexPattern;
    }
}