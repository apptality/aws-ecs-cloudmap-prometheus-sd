namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Represent object containing information about metadata labels
/// </summary>
public class DiscoveryLabel : IEquatable<DiscoveryLabel>
{
    /// <summary>
    /// Label name
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Label value
    /// </summary>
    public string Value { get; init; } = null!;

    /// <summary>
    /// Label priority
    /// </summary>
    public DiscoveryLabelPriority Priority { get; init; }

    public bool Equals(DiscoveryLabel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // It might seem controversial to omit Priority from the comparison,
        // but it only used as a marker for the label source
        return Name == other.Name && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((DiscoveryLabel)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Value);
    }
}