using System.ComponentModel.DataAnnotations;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Class for configuring service discovery options
/// </summary>
public sealed class DiscoveryOptions
{
    /// <summary>
    /// Semicolon separated string containing ECS clusters names to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of "EcsClusters" or "CloudMapNamespaces" must be provided.
    /// </remarks>
    public string EcsClusters { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of tag key-value pairs to select ECS services.
    /// If a selector is provided, service must match all tags in the selector.
    /// If selector is not provided, all services in the clusters are included.
    /// Tag value selector supports wildcard symbol: *.
    /// Tags key/value matching is case-sensitive.
    /// <br/>
    /// Example:
    ///    Given the following selector: "service_discovery=true;component=app;other_tag=*;"
    ///    All services with the tag "service_discovery" set to "true"
    ///    and "component" set to "app"
    ///    and "other_tag" set to any value will be included.
    /// <br/>
    /// IMPORTANT:
    ///    1. It is your responsibility to ensure that all tags are present on the resource
    ///    2. If you specify 'CloudMapServiceSelectorTags' - it is your responsibility to ensure
    ///       that ecs services and corresponding cloud map services are both included via selectors specified.
    /// </summary>
    public string EcsServiceSelectorTags { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing Cloud Map namespaces names to query for services.
    /// All ECS services that are using Service Connect are always included in lookup.
    /// This parameter allows you to include additional namespaces to discover
    /// services using AWS Service Discovery (legacy).
    /// </summary>
    /// <remarks>
    /// At least one of "EcsClusters" or "CloudMapNamespaces" must be provided.
    /// </remarks>
    public string CloudMapNamespaces { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of tag key-value pairs to select CloudMap services.
    /// If selector is not provided, all services in the CloudMap namespace are included.
    /// Example: "service_discovery=true;component=app"
    /// Default is empty string (match all services).
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html">service-connect</a> docs.
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html">service-discovery</a> docs.
    /// </remarks>
    public string CloudMapServiceSelectorTags { get; set; } = string.Empty;

    // Tags are resolved with the following priority:
    // 1) ECS Task
    // 2) ECS Service
    // 3) Cloud Map Service
    // 4) Cloud Map Namespace
    // For example, if both Cloud Map Namespace and ECS Task have a tag
    // with the same key, the value from the ECS Task will be used.

    /// <summary>
    /// When non-empty, tags that match from ECS tasks
    /// are included in the service discovery results as labels.
    /// </summary>
    /// <remarks>
    /// Note: tags are resolved from running tasks, not tasks definitions.
    /// If you would like to include tags from task definitions,
    /// consider modifying ECS service to propagate tags from task definitions to running tasks.
    /// You can read more <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ecs-using-tags.html#tag-resources">here</a>
    /// </remarks>
    public string EcsTaskTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from ECS services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string EcsServiceTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from Cloud Map services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapServiceTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from Cloud Map namespaces
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapNamespaceTags { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of labels to include in the service discovery response as metadata.
    /// Will be added to all discovered targets.
    /// </summary>
    /// <remarks>
    /// For example, "environment=dev;region=us-west-2"
    /// If value needs to have a semicolon, or equals sign, it must be prefixed using %.
    /// For example, "eval_expression=a%=%=b;components=a%;b%;;"
    /// would be parsed as "eval_expression=a==b;components=a;b;"
    /// Any label that is not a valid Prometheus label will be transformed to such.
    /// Read more: https://prometheus.io/docs/prometheus/latest/configuration/configuration/#labelname
    /// Example:
    ///    "static_key_1=static_value_1;static_key_2=static_value_2;"
    /// </remarks>
    public string ExtraPrometheusLabels { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how long to cache the service discovery results in seconds. Default is five seconds.
    /// </summary>
    /// <remarks>
    /// When caching is disabled, the service discovery results are queried every time a service response is requested.
    /// Min value - 0 seconds (no caching). Max value - 65535 seconds.
    /// <para>
    /// <b>Important:</b> It is strongly recommended to keep caching at the very least for 5 seconds to avoid rate limiting.
    /// </para>
    /// </remarks>
    [Range(0, 65535)]
    public ushort CacheTtlSeconds { get; set; } = 60;

    /// <summary>
    /// When set to 'true', top-level resources (clusters, namespaces) will be cached.
    /// This significantly reduces the number of requests to AWS, but may result in stale data.
    /// Data is cached for 10 minutes.
    /// </summary>
    public bool CacheTopLevelResources { get; set; } = true;

    /// <summary>
    /// Tag prefix to identify metrics port, [path | "/metrics"], [name | ""] triplets.
    /// Must be at least three characters long, start with a letter, and end with an underscore.
    /// </summary>
    /// <remarks>
    /// This prefix has a special meaning and is used to identify the metrics path, port, and name
    /// for the service scrape configuration. In a single ecs service, you may have multiple
    /// containers listening on different ports, possibly having a different path you need to scrape from.
    /// <para>
    /// 'PATH', 'PORT', 'NAME' will be appended to the prefix to identify
    /// metrics path, port, and scrape configuration names respectively.
    /// If your service contains multiple metrics paths
    /// or ports, you can tag your resources using the following format:
    /// <ul>
    /// <li>METRICS_PATH_1 = /metrics</li>
    /// <li>METRICS_PORT_1 = 9001</li>
    /// <li>METRICS_NAME_1 = application</li>
    /// </ul>
    /// <ul>
    /// <li>METRICS_PATH_2 = /metrics2</li>
    /// <li>METRICS_PORT_2 = 9002</li>
    /// <li>METRICS_NAME_2 = sidecar</li>
    /// </ul>
    /// ..etc.
    /// </para>
    /// <para>
    /// System will not care of anything after _PATH, _PORT, or _NAME,
    /// so as long as these postfixes are identical for triplets,
    /// you can use any postfix (including empty) you prefer,
    /// which makes the following combination valid:
    /// <ul>
    /// <li>METRICS_PATH = /metrics</li>
    /// <li>METRICS_PORT = 9001</li>
    /// </ul>
    /// <ul>
    /// <li>METRICS_PATH_SVC-ONE = /metrics</li>
    /// <li>METRICS_PORT_SVC-ONE = 9001</li>
    /// <li>METRICS_NAME_SVC-ONE = svc-one</li>
    /// </ul>
    /// </para>
    /// If a path is not provided, default path "/metrics" will be used.
    /// <br />
    /// If a name is not provided, the service name will be omitted from labels ('scrape_target_name' label).
    /// <br />
    /// This directly affects the Prometheus http sd scrape configuration:
    /// <code>
    /// [{
    ///    "targets": [
    ///      "10.200.10.200:${METRICS_PORT}"
    ///    ],
    ///    "labels": {
    ///      "__metrics_path__": "${METRICS_PATH}",
    ///      "scrape_target_name": "${METRICS_NAME}",
    ///      ...
    ///     }
    /// }]
    /// </code>
    /// </remarks>
    [Required]
    [RegularExpression(@"^[a-zA-Z]{1}[\w-]+[_]{1}$")]
    public string MetricsPathPortTagPrefix { get; set; } = "METRICS_";

    /// <summary>
    /// Semicolon separated string of relabel configurations to apply to the scrape configuration result.
    /// This allows using simple syntax to create new labels with existing values,
    /// or to combine multiple pre-calculated labels into a single label.
    /// </summary>
    /// <remarks>
    /// The intended use case is to create new labels based on existing labels, so that you don't have to
    /// modify your Grafana Dashboards or alerting rules when you change the way you tag your resources.
    /// <br />
    /// Relabel configurations are applied to the scrape configuration before it is returned to the client.
    /// You can only operate using labels that are already present in the target.
    /// <br />
    /// Example:
    /// Consider the following response of discovery target for configuration without relabeling:
    /// <code>
    ///  {
    ///        "targets": ["10.200.10.200:8080"],
    ///        "labels": {
    ///            "__metrics_path__": "/metrics",
    ///            "instance": "10.200.10.200",
    ///            "scrape_target_name": "app",
    ///            "ecs_cluster": "my-cluster",
    ///            "ecs_service": "my-service",
    ///            ...
    ///        }
    ///    },
    /// </code>
    /// By applying the following relabel configuration:
    /// <code>
    /// "service_and_cluster={{ecs_cluster}}-{{ecs_service}};"
    /// </code>
    /// This will create a new label 'service_and_cluster' with the value of 'ecs_cluster' and 'ecs_service' labels combined:
    /// <code>
    ///  {
    ///        "targets": ["10.200.10.200:8080"],
    ///        "labels": {
    ///            "__metrics_path__": "/metrics",
    ///            "instance": "10.200.10.200",
    ///            "scrape_target_name": "app",
    ///            "ecs_cluster": "my-cluster",
    ///            "ecs_service": "my-service",
    ///            "service_and_cluster": "my-cluster-my-service",
    ///            ...
    ///        }
    ///    },
    /// </code>
    /// <br />
    /// If your intent is to only 'rename' the existing label, you can use the following syntax:
    /// <code>
    /// "cluster={{ecs_cluster}};"
    /// </code>
    /// This will create new label 'cluster' with the value of 'ecs_cluster' label.
    /// <br />
    /// IMPORTANT:
    /// <ul>
    /// <li>1) If amy of the replacement tokens are not found in the source labels, it will not be replaced at all.</li>
    /// <li>2) You can replace existing labels ("ecs_cluster=my-{{ecs_cluster}};").</li>
    /// <li>3) All relabel configurations are applied in order defined.</li>
    /// <li>4) You can have multiple relabel configurations for the same label. They will be applied in order defined ("a=1;a={{a}}-2;" will result in a="1-2").</li>
    /// </ul>
    /// <br/>
    /// To have a fall-backs, use 'ExtraPrometheusLabels' option. These have the lowest priority,
    /// and are only added to the response if the label is not already present in the target
    /// (matching tag is not present in any of the original resources).
    /// </remarks>
    public string RelabelConfigurations { get; set; } = string.Empty;
}