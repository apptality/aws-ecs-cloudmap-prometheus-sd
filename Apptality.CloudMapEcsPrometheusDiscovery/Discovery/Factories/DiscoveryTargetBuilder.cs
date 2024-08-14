using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Factories;

/// <summary>
/// Builder for creating DiscoveryTarget objects
/// </summary>
public class DiscoveryTargetBuilder
{
    private readonly DiscoveryTarget _discoveryTarget = new();

    public DiscoveryTargetBuilder WithCloudMapServiceName(string cloudMapServiceName)
    {
        _discoveryTarget.CloudMapServiceName = cloudMapServiceName;
        return this;
    }

    public DiscoveryTargetBuilder WithCloudMapServiceInstanceId(string cloudMapServiceInstanceId)
    {
        _discoveryTarget.CloudMapServiceInstanceId = cloudMapServiceInstanceId;
        return this;
    }

    public DiscoveryTargetBuilder WithCloudMapServiceType(CloudMapServiceType? cloudMapServiceType)
    {
        if (cloudMapServiceType.HasValue)
            _discoveryTarget.ServiceType = cloudMapServiceType.Value;
        return this;
    }

    public DiscoveryTargetBuilder WithIpAddress(string ipAddress)
    {
        _discoveryTarget.IpAddress = ipAddress;
        return this;
    }

    public DiscoveryTargetBuilder WithEcsCluster(string ecsCluster)
    {
        _discoveryTarget.EcsCluster = ecsCluster;
        return this;
    }

    public DiscoveryTargetBuilder WithEcsService(string ecsService)
    {
        _discoveryTarget.EcsService = ecsService;
        return this;
    }

    public DiscoveryTargetBuilder WithEcsTaskDefinitionArn(string ecsTaskDefinitionArn)
    {
        _discoveryTarget.EcsTaskDefinitionArn = ecsTaskDefinitionArn;
        return this;
    }

    public DiscoveryTargetBuilder WithEcsTaskArn(string ecsTaskArn)
    {
        _discoveryTarget.EcsTaskArn = ecsTaskArn;
        return this;
    }

    public DiscoveryTargetBuilder WithScrapeConfigurations(ICollection<ScrapeConfiguration> scrapeConfigurations)
    {
        _discoveryTarget.ScrapeConfigurations.AddRange(scrapeConfigurations);
        return this;
    }

    public DiscoveryTargetBuilder WithLabels(ICollection<DiscoveryLabel> labels)
    {
        // We can only add labels with the same Name of a higher priority
        // (lower value of DiscoveryLabelPriority property)
        foreach (var label in labels)
        {
            var existingLabel = _discoveryTarget.Labels.FirstOrDefault(l => l.Name == label.Name);
            if (existingLabel != null && existingLabel.Priority <= label.Priority) continue;
            // remove label with lower priority and add new label
            _discoveryTarget.Labels.RemoveAll(l => l.Name == label.Name);
            _discoveryTarget.Labels.Add(label);
        }

        return this;
    }

    public DiscoveryTargetBuilder WithoutLabels(ICollection<DiscoveryLabel> labels)
    {
        _discoveryTarget.Labels.RemoveAll(l => labels.Select(lb => lb.Name).Contains(l.Name));
        return this;
    }

    public ICollection<DiscoveryLabel> Labels => _discoveryTarget.Labels;

    public DiscoveryTarget Build()
    {
        return _discoveryTarget;
    }
}