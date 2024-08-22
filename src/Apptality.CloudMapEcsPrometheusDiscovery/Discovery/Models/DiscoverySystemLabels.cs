namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// System labels directly inferred from the AWS resource properties
/// </summary>
internal abstract class DiscoverySystemLabel : DiscoveryLabel
{
    const string LabelPrefix = "_sys_";
    public string OriginalName { get; init; }

    protected DiscoverySystemLabel(string name, string value)
    {
        OriginalName = name;
        Name = $"{LabelPrefix}{name}";
        Value = value;
        Priority = DiscoveryLabelPriority.System;
    }
}

/// <summary>
/// Label for metadata information. These then can be used for relabelling in Prometheus
/// </summary>
/// <remarks>
/// Read more at:
/// <ul>
/// <li><a href="https://prometheus.io/docs/prometheus/latest/configuration/configuration/#relabel_config">Prometheus Documentation</a></li>
/// <li><a href="https://www.robustperception.io/life-of-a-label/">Life of a Label</a></li>
/// </ul>
/// </remarks>
internal class DiscoveryMetadataLabel : DiscoveryLabel
{
    const string LabelPrefix = "__meta_";

    private DiscoveryMetadataLabel(string value)
    {
        Value = value;
        Priority = DiscoveryLabelPriority.Metadata;
    }

    public DiscoveryMetadataLabel(string name, string value) : this(value)
    {
        Name = $"{LabelPrefix}{name}";
    }

    public DiscoveryMetadataLabel(DiscoveryLabel label) : this(label.Value)
    {
        Name = $"{LabelPrefix}{label.Name}";
    }

    public DiscoveryMetadataLabel(DiscoverySystemLabel label) : this(label.Value)
    {
        Name = $"{LabelPrefix}{label.OriginalName}";
    }
}

internal class EcsClusterDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("ecs_cluster", value.EcsCluster);

internal class EcsServiceDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("ecs_service", value.EcsService);

internal class EcsTaskDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("ecs_task", value.EcsTaskArn);

internal class EcsTaskDefinitionDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("ecs_task_definition", value.EcsTaskDefinitionArn);

internal class CloudMapServiceDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("cloudmap_service_name", value.CloudMapServiceName);

internal class CloudMapServiceInstanceDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("cloudmap_service_instance_id", value.CloudMapServiceInstanceId);

internal class CloudMapServiceTypeDiscoverySystemLabel(DiscoveryTarget value)
    : DiscoverySystemLabel("cloudmap_service_type", value.ServiceType?.ToString() ?? "");