using System.Text.Json.Serialization;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Responses;

/// <summary>
/// Model representing a Prometheus HTTP service discovery response
/// </summary>
/// <remarks>
/// Read more at <a href="https://prometheus.io/docs/prometheus/latest/configuration/configuration/#http_sd_config">prometheus.io</a>
/// </remarks>
public class PrometheusTargetsResponse : List<StaticConfigResponse>;

/// <summary>
/// Model representing a Prometheus HTTP service discovery static configuration entry
/// </summary>
public class StaticConfigResponse
{
    [JsonPropertyName("targets")]
    public List<string> Targets { get; set; } = [];

    [JsonPropertyName("labels")]
    public Dictionary<string,string> Labels { get; set; } = new();
}

// TODO: Remove
// [
//    {
//       "targets": [
//          "10.200.65.201:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.201",
//          "namespace": "cloudmap.namespace",
//          "service": "service-jobs",
//          "taskdefinition": "ecs-service-jobs",
//          "taskid": "92868bcd4b104954b487c21cc1033f8c"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.201:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.201",
//          "namespace": "cloudmap.namespace",
//          "service": "service-jobs",
//          "taskdefinition": "ecs-service-jobs",
//          "taskid": "92868bcd4b104954b487c21cc1033f8c"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.104:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.104",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http-ui-host",
//          "taskdefinition": "ecs-service-http-ui-host",
//          "taskid": "7d5e80c93501464e8843b10bc20c185d"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.104:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.104",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http-ui-host",
//          "taskdefinition": "ecs-service-http-ui-host",
//          "taskid": "7d5e80c93501464e8843b10bc20c185d"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.167:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.167",
//          "namespace": "cloudmap.namespace",
//          "service": "service-grpc",
//          "taskdefinition": "ecs-service-grpc",
//          "taskid": "6f2cbb195e3d471ba57b9bb8ec241682"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.167:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.167",
//          "namespace": "cloudmap.namespace",
//          "service": "service-grpc",
//          "taskdefinition": "ecs-service-grpc",
//          "taskid": "6f2cbb195e3d471ba57b9bb8ec241682"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.58:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.58",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http",
//          "taskdefinition": "ecs-service-http",
//          "taskid": "baf2a3419ee446c587cba75766d899f9"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.58:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.58",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http",
//          "taskdefinition": "ecs-service-http",
//          "taskid": "baf2a3419ee446c587cba75766d899f9"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.151:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.151",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http",
//          "taskdefinition": "ecs-service-http",
//          "taskid": "e3dab68c211d4dc187549dfa0a8f5369"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.151:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.151",
//          "namespace": "cloudmap.namespace",
//          "service": "service-http",
//          "taskdefinition": "ecs-service-http",
//          "taskid": "e3dab68c211d4dc187549dfa0a8f5369"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.120:8080"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.120",
//          "namespace": "cloudmap.namespace",
//          "service": "connector-app",
//          "taskdefinition": "ecs-connector-app",
//          "taskid": "5ad776f381d947ffb6f6d60d77912ce5"
//       }
//    },
//    {
//       "targets": [
//          "10.200.65.120:9779"
//       ],
//       "labels": {
//          "__metrics_path__": "/metrics",
//          "cluster": "ecs-cluster-name",
//          "instance": "10.200.65.120",
//          "namespace": "cloudmap.namespace",
//          "service": "connector-app",
//          "taskdefinition": "ecs-connector-app",
//          "taskid": "5ad776f381d947ffb6f6d60d77912ce5"
//       }
//    }
// ]