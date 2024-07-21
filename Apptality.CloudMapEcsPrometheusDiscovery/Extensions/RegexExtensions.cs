namespace Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

/// <summary>
/// Contains extension methods for working with regular expressions
/// </summary>
internal static class RegexExtensions
{
    /// <summary>
    /// Validates a regex
    /// </summary>
    internal static bool IsValidRegex(this string regex)
    {
        try
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            System.Text.RegularExpressions.Regex.Match("", regex);
            return true;
        }
        catch
        {
            return false;
        }
    }
}