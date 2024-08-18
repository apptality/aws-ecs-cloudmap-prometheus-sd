using System.Text.Json.Serialization;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;

/// <summary>
/// Class that provides a custom JSON serializer context for the PrometheusResponse model,
/// that is using source-generated serialization code, thus not needing reflection.
/// </summary>
[JsonSerializable(typeof(List<StaticConfigResponse>))]
[JsonSerializable(typeof(StaticConfigResponse))]
[JsonSerializable(typeof(PrometheusResponse))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
internal partial class PrometheusResponseSerializerContext : JsonSerializerContext
{
}