namespace Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;

/// <summary>
/// Model representing a Prometheus HTTP service discovery response
/// </summary>
/// <remarks>
/// Read more at <a href="https://prometheus.io/docs/prometheus/latest/configuration/configuration/#http_sd_config">prometheus.io</a>
/// </remarks>
public class PrometheusResponse : List<StaticConfigResponse>;

/// <summary>
/// Model representing a Prometheus HTTP service discovery static configuration entry
/// </summary>
public class StaticConfigResponse
{
    public List<string> Targets { get; set; } = [];
    public Dictionary<string, string> Labels { get; set; } = new();
}