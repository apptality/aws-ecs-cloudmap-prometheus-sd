using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Factories;

/// <summary>
/// Builder for creating DiscoveryTarget objects
/// </summary>
public class DiscoveryTargetBuilder
{
    private readonly DiscoveryTarget _discoveryTarget = new();

    private readonly List<RelabelConfiguration> _relabelConfigurations = [];

    public ICollection<DiscoveryLabel> Labels => _discoveryTarget.Labels;

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
            // Try to find an existing label with the same name
            var existingLabel = _discoveryTarget.Labels.FirstOrDefault(l =>
                string.Equals(l.Name, label.Name, StringComparison.OrdinalIgnoreCase));
            // If the existing label has a lower priority, skip it
            if (existingLabel != null)
            {
                if (existingLabel.Priority < label.Priority) continue;
                // Remove the existing label if it's less important
                _discoveryTarget.Labels.Remove(existingLabel);
            }

            // Add the new label
            _discoveryTarget.Labels.Add(label);
        }

        return this;
    }

    public DiscoveryTargetBuilder WithoutLabels(ICollection<DiscoveryLabel> labels)
    {
        _discoveryTarget.Labels.RemoveAll(l => labels.Select(lb => lb.Name.ToLower()).Contains(l.Name.ToLower()));
        return this;
    }

    public DiscoveryTargetBuilder WithRelabelConfigurations(ICollection<RelabelConfiguration> relabelConfigurations)
    {
        _relabelConfigurations.AddRange(relabelConfigurations);
        return this;
    }

    public DiscoveryTarget Build()
    {
        // Add system labels
        ApplySystemLabels();

        // Apply re-label configurations
        ApplyRelabelConfigurations();

        return _discoveryTarget;
    }

    /// <summary>
    /// Adds system labels to the target - these are inferred from most valuable information
    /// </summary>
    private void ApplySystemLabels()
    {
        // Store system labels
        List<DiscoverySystemLabel> systemLabels =
        [
            new EcsClusterDiscoverySystemLabel(_discoveryTarget),
            new EcsServiceDiscoverySystemLabel(_discoveryTarget),
            new EcsTaskDiscoverySystemLabel(_discoveryTarget),
            new EcsTaskDefinitionDiscoverySystemLabel(_discoveryTarget),
            new CloudMapServiceDiscoverySystemLabel(_discoveryTarget),
            new CloudMapServiceInstanceDiscoverySystemLabel(_discoveryTarget),
            new CloudMapServiceTypeDiscoverySystemLabel(_discoveryTarget)
        ];

        // Cast system labels to DiscoveryLabel
        var labels = systemLabels.Select(DiscoveryLabel (l) => l).ToList();

        WithLabels(labels);

        // Cast system labels to DiscoveryMetadataLabel and provide these values as metadata to allow relabelling in Prometheus
        labels = systemLabels.Select(DiscoveryLabel (l) => new DiscoveryMetadataLabel(l)).ToList();

        WithLabels(labels);
    }

    /// <summary>
    /// Applies relabel configurations to the target
    /// </summary>
    private void ApplyRelabelConfigurations()
    {
        foreach (var relabelConfiguration in _relabelConfigurations)
        {
            var label = relabelConfiguration.ToDiscoveryLabel(_discoveryTarget.Labels);
            if (label == null) continue;
            // It is very important to add the label at each iteration,
            // because the same label can be re-labeled multiple times,
            // or it may be used in next replacement patterns.
            WithLabels([label]);
        }

        var temporaryLabelsKeys = _relabelConfigurations
            .Where(l => l.Temporary)
            .Select(l => l.TargetLabelName)
            .ToList();

        // Remove temporary labels (these are a byproduct of re-labelling), if any
        _discoveryTarget.Labels.RemoveAll(
            l => l.Name.StartsWith("_tmp", StringComparison.OrdinalIgnoreCase) && temporaryLabelsKeys.Contains(l.Name)
        );
    }
}