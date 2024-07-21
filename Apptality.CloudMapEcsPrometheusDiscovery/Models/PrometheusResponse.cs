using System.Collections;
using System.Text.Json.Serialization;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Models;

/// <summary>
/// Model representing a Prometheus HTTP service discovery response
/// </summary>
/// <remarks>
/// Read more at <a href="https://prometheus.io/docs/prometheus/latest/configuration/configuration/#http_sd_config">prometheus.io</a>
/// </remarks>
public class PrometheusResponse : List<PrometheusStaticConfig>;

public class PrometheusStaticConfig
{
    [JsonPropertyName("targets")]
    public IList<string> Targets { get; set; } = new List<string>();

    [JsonPropertyName("labels")]
    public Dictionary<string,string> Labels { get; set; } = new Dictionary<string, string>();
}