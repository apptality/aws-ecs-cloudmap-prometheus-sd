using System.Security.Cryptography;
using System.Text;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

public static class HashingExtensions
{
    /// <summary>
    /// Based on the provided values, computes a SHA256 hash.
    /// If the collection is null or empty, returns "empty"
    /// </summary>
    public static string ComputeHash(this ICollection<string>? values)
    {
        // If collection is empty or null - short circuit
        if (values is not {Count: > 0}) return "empty";
        // Combine all strings into one string with a delimiter
        var combinedString = string.Join("|", values);
        // Compute the hash as a byte array
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
        // Convert the byte array to a hex string
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}