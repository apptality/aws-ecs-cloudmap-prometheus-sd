namespace Apptality.CloudMapEcsPrometheusDiscovery.Models;

/// <summary>
/// Object representing a Prometheus Static Config
/// </summary>
public class PrometheusStaticConfig
{
    public required PrometheusTarget Target { get; init; }
    public List<PrometheusLabel> Labels { get; init; } = [];
}

/// <summary>
/// Object representing a Prometheus target
/// </summary>
public class PrometheusTarget(string Host, ushort Port);

/// <summary>
/// Object representing a Prometheus label
/// </summary>
public record PrometheusLabel(string Name, string Value);