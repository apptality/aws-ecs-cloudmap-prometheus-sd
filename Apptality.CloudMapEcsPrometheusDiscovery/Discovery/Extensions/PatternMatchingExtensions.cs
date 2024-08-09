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
    /// <param name="values">
    /// Array of values to match against patterns
    /// </param>
    /// <param name="patterns">
    /// Array of patterns to match against values
    /// </param>
    /// <returns>
    /// Any value that matches any of the patterns
    /// </returns>
    public static IEnumerable<string> Match(IEnumerable<string> values, IEnumerable<string> patterns)
    {
        var regexPatterns = patterns.Where(s => !string.IsNullOrWhiteSpace(s)).Select(ConvertToRegex).ToList();
        return values.Where(value => regexPatterns.Any(pattern => Regex.IsMatch(value, pattern)));
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
    private static string ConvertToRegex(string pattern)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return "^" + Regex.Escape(pattern) + "$";
        }

        var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return regexPattern;
    }
}