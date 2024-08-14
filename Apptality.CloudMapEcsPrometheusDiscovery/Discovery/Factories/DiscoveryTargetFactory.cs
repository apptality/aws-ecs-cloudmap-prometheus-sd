using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Extensions;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Factories;

/// <summary>
/// Factory for creating DiscoveryTarget objects from a DiscoveryResult
/// </summary>
public class DiscoveryTargetFactory : IDiscoveryTargetFactory
{
    /// <inheritdoc cref="IDiscoveryTargetFactory.Create"/>
    public ICollection<DiscoveryTarget> Create(DiscoveryResult discoveryResult)
    {
        var discoveryTargets = new List<DiscoveryTarget>();

        var discoveryOptions = discoveryResult.DiscoveryOptions;

        // Pre-select CloudMap services to ease filtering
        var cloudMapServices = discoveryResult.CloudMapNamespaces
            .SelectMany(x => x.ServiceSummaries)
            .ToList();

        // Iterate over each ECS cluster and match it with the CloudMap services
        foreach (var ecsCluster in discoveryResult.EcsClusters)
        {
            // Iterate over each ECS service in the cluster
            foreach (var ecsService in ecsCluster.Services)
            {
                // Find matching CloudMap service
                var ecsCloudMapService = FindMatchingCloudMapService(ecsService, cloudMapServices);

                foreach (var ecsTask in ecsService.Tasks)
                {
                    // Short-circuit non-running tasks
                    if (!ecsTask.IsRunning()) continue;

                    // Create a builder with the extra labels from the options
                    var builder = CreateDefaultBuilder(discoveryOptions);

                    // Build information about the ECS task, service and cluster
                    BuildEcsInformation(builder, discoveryOptions, ecsTask, ecsService, ecsCluster);

                    // Build information about the CloudMap service
                    BuildCloudMapInformation(builder, discoveryOptions, ecsCloudMapService, ecsTask.IpAddress());

                    // Build scrape configurations based on the discovery result and the labels aggregated
                    BuildScrapeConfigurations(builder, discoveryOptions);

                    discoveryTargets.Add(builder.Build());
                }
            }
        }

        return discoveryTargets;
    }

    /// <summary>
    /// Augments the builder with ECS information
    /// </summary>
    internal void BuildEcsInformation(
        DiscoveryTargetBuilder builder,
        DiscoveryOptions discoveryOptions,
        EcsTask ecsTask,
        EcsService ecsService,
        EcsCluster ecsCluster
    )
    {
        // Add the ECS cluster and service labels
        builder.WithEcsCluster(ecsCluster.Cluster.ClusterName);
        builder.WithEcsService(ecsService.Service.ServiceName);
        builder.WithEcsTaskArn(ecsTask.Task.TaskArn);
        builder.WithEcsTaskDefinitionArn(ecsTask.Task.TaskDefinitionArn);
        builder.WithIpAddress(ecsTask.IpAddress());

        // ECS Tags -> Labels
        var ecsTaskLabels = BuildLabelsFromTags(
            ecsTask.Tags,
            discoveryOptions.GetEcsTaskTagFilters(),
            DiscoveryLabelPriority.EcsTask
        );
        builder.WithLabels(ecsTaskLabels);

        var ecsServiceLabels = BuildLabelsFromTags(
            ecsService.Tags,
            discoveryOptions.GetEcsServiceTagFilters(),
            DiscoveryLabelPriority.EcsService
        );
        builder.WithLabels(ecsServiceLabels);
    }

    /// <summary>
    /// Augments the builder with CloudMap information
    /// </summary>
    internal static void BuildCloudMapInformation(
        DiscoveryTargetBuilder builder,
        DiscoveryOptions discoveryOptions,
        CloudMapService? ecsCloudMapService,
        string ecsTaskIpAddress
    )
    {
        // See if we have a CloudMap Service Connect service for this task
        if (ecsCloudMapService == null) return;

        // Add the CloudMap namespace and service instance labels
        builder.WithCloudMapServiceName(ecsCloudMapService.ServiceSummary.Name);
        builder.WithCloudMapServiceType(ecsCloudMapService.ServiceType);

        // Only add the instance ID if it was found
        if (ecsCloudMapService.FindInstanceByIp(ecsTaskIpAddress) is { } cloudMapServiceInstance)
        {
            builder.WithCloudMapServiceInstanceId(cloudMapServiceInstance.Id);
        }

        // CloudMap Tags -> Labels
        var cloudMapServiceLabels = BuildLabelsFromTags(
            ecsCloudMapService.Tags,
            discoveryOptions.GetCloudMapServiceTagFilters(),
            DiscoveryLabelPriority.CloudMapService
        );
        builder.WithLabels(cloudMapServiceLabels);

        var cloudMapNamespaceLabels = BuildLabelsFromTags(
            ecsCloudMapService.CloudMapNamespace.Tags,
            discoveryOptions.GetCloudMapNamespaceTagFilters(),
            DiscoveryLabelPriority.CloudMapNamespace
        );
        builder.WithLabels(cloudMapNamespaceLabels);
    }

    /// <summary>
    /// Based on the discovery result and the labels aggregated,
    /// creates a collection of scrape configurations
    /// </summary>
    internal static void BuildScrapeConfigurations(
        DiscoveryTargetBuilder builder,
        DiscoveryOptions discoveryOptions
    )
    {
        var metricsPathPortPrefix = discoveryOptions.MetricsPathPortTagPrefix;

        // Build the regex patterns for matching metrics path and port labels
        var metricsPathPattern = $"{metricsPathPortPrefix}PATH*".ConvertToRegexString();
        var metricsPortPattern = $"{metricsPathPortPrefix}PORT*".ConvertToRegexString();
        var metricsNamePattern = $"{metricsPathPortPrefix}NAME*".ConvertToRegexString();

        // Now match all labels that match the pattern
        var metricsPathPortLabels = builder.Labels
            .Where(label => label.Name.Match([metricsPathPattern, metricsPortPattern, metricsNamePattern]))
            .ToList();

        // Now we need to group these into triplets of the metrics path, port, and name.
        const string emptyGroupToken = "<empty>";
        var metricsPathPortPairs = metricsPathPortLabels
            .GroupBy(label => label.Name.Split('_').Last() switch
            {
                // If the trailing part is 'PATH', 'PORT', or 'NAME'
                // then we will substitute it with the empty group token
                "PATH" => emptyGroupToken,
                "PORT" => emptyGroupToken,
                "NAME" => emptyGroupToken,
                // Otherwise, we'll use the suffix as is
                _ => label.Name.Split('_').Last()
            })
            .Select(group => group.ToList())
            .ToList();

        // If no pairs were found, return an empty collection
        if (metricsPathPortPairs.Count == 0) return;

        // Extract the metrics path and port from the pairs into ScrapeConfigurations
        var scrapeConfigurations = metricsPathPortPairs
            .Select(pair =>
            {
                var pathString = pair.FirstOrDefault(label => label.Name.Contains("_PATH"))?.Value ?? "/metrics";
                var nameString = pair.FirstOrDefault(label => label.Name.Contains("_NAME"))?.Value ?? string.Empty;
                var portString = pair.FirstOrDefault(label => label.Name.Contains("_PORT"))?.Value ?? string.Empty;
                if (!ushort.TryParse(portString, out var port)) return null;
                return new ScrapeConfiguration {MetricsPath = pathString, Port = port, Name = nameString};
            })
            .ToList();

        // Remove path/port labels from the builder,
        // as these were only required to build the scrape configurations
        builder.WithoutLabels(metricsPathPortLabels);

        // Add the scrape configurations
        builder.WithScrapeConfigurations(scrapeConfigurations.Where(sc => sc != null).ToList()!);
    }

    /// <summary>
    /// Creates a default builder with the extra labels from the options
    /// </summary>
    internal DiscoveryTargetBuilder CreateDefaultBuilder(DiscoveryOptions options)
    {
        var builder = new DiscoveryTargetBuilder();
        // Add extra labels
        builder.WithLabels(options.GetExtraPrometheusLabels());
        return builder;
    }

    /// <summary>
    /// Finds a matching CloudMap service for the given ECS service
    /// </summary>
    /// <param name="ecsService">ECS service to find matching CloudMap service for</param>
    /// <param name="cloudMapServices">Collection to look matching service in</param>
    /// <returns>Instance of <see cref="CloudMapService"/> if found, null - otherwise</returns>
    internal CloudMapService? FindMatchingCloudMapService(
        EcsService? ecsService,
        ICollection<CloudMapService>? cloudMapServices
    )
    {
        // Short-circuit if either of the inputs is null
        if (ecsService == null || cloudMapServices == null) return null;

        // Declare a placeholder for the CloudMap service
        CloudMapService? ecsCloudMapService;

        // See if Ecs Service has CloudMap Service Connect service
        var mostRecentEcsServiceDeployment = ecsService.Service.Deployments
            .Where(t => t.Status == "PRIMARY").MaxBy(t => t.CreatedAt);

        var cloudMapServiceConnectServices = mostRecentEcsServiceDeployment?
            .ServiceConnectResources.Select(scr => scr.DiscoveryArn).ToList();

        if (cloudMapServiceConnectServices is {Count: > 0})
        {
            ecsCloudMapService = cloudMapServices
                .Where(x => x.ServiceType == CloudMapServiceType.ServiceConnect)
                .FirstOrDefault(x => cloudMapServiceConnectServices?.Contains(x.ServiceSummary.Arn) ?? false);
        }
        else
        {
            // See if Ecs Service has CloudMap Service Discovery service
            ecsCloudMapService = cloudMapServices
                .Where(x => x.ServiceType == CloudMapServiceType.ServiceDiscovery)
                .FirstOrDefault(x => x.InstanceSummaries.Any(s =>
                    s.EcsClusterName() == ecsService.ClusterName &&
                    s.EcsServiceName() == ecsService.ServiceName)
                );
        }

        return ecsCloudMapService;
    }

    /// <summary>
    /// Build discovery labels from tags
    /// </summary>
    /// <param name="resourceTags">
    /// Tags to build labels from
    /// </param>
    /// <param name="tagKeysFilters">
    /// Tag keys filters.
    /// Only tags "Key" property matching these filters will be included.
    /// </param>
    /// <param name="priority">
    /// Priority of the labels to assign
    /// </param>
    internal static ICollection<DiscoveryLabel> BuildLabelsFromTags(
        ICollection<ResourceTag> resourceTags,
        ICollection<string> tagKeysFilters,
        DiscoveryLabelPriority priority)
    {
        if (tagKeysFilters.Count == 0) return [];
        var labels = resourceTags
            .Where(tag => tag.Key.Match(tagKeysFilters))
            .Select(tag => new DiscoveryLabel
            {
                Name = tag.Key,
                Value = tag.Value,
                Priority = priority
            })
            .ToList();

        return labels;
    }
}