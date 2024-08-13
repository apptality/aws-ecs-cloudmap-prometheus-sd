using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
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
                // Declare a placeholder for the CloudMap service
                CloudMapService? ecsCloudMapService = null;

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
                            s.EcsClusterName() == ecsCluster.ClusterName &&
                            s.EcsServiceName() == ecsService.Service.ServiceName)
                        );
                }

                foreach (var ecsTask in ecsService.Tasks)
                {
                    // Short-circuit non-running tasks
                    if (!ecsTask.IsRunning()) continue;

                    // Create a builder with the extra labels from the options
                    var builder = CreateDefaultBuilder(discoveryResult.DiscoveryOptions);

                    // Add the ECS cluster and service labels
                    builder.WithEcsCluster(ecsCluster.Cluster.ClusterName);
                    builder.WithEcsService(ecsService.Service.ServiceName);
                    builder.WithEcsTaskArn(ecsTask.Task.TaskArn);
                    builder.WithEcsTaskDefinitionArn(ecsTask.Task.TaskDefinitionArn);

                    var ipAddress = ecsTask.IpAddress();
                    builder.WithIpAddress(ipAddress);

                    // CloudMap

                    // Declare a placeholder for the CloudMap service instance
                    CloudMapServiceInstance? associatedCloudMapServiceInstance = null;

                    // See if we have a CloudMap Service Connect service for this task
                    if (ecsCloudMapService != null)
                    {
                        // CloudMap Service is a 'Service Connect' service
                        var cloudMapServiceInstances = ecsCloudMapService.FindInstanceByIp(ipAddress);
                        if (cloudMapServiceInstances.Count > 0)
                        {
                            associatedCloudMapServiceInstance = cloudMapServiceInstances.First();
                        }

                        // Only fill these in if we have a CloudMap service instance
                        if (associatedCloudMapServiceInstance != null)
                        {
                            // Add the CloudMap namespace and service instance labels
                            builder.WithCloudMapServiceName(ecsCloudMapService.ServiceSummary.Name);
                            builder.WithCloudMapServiceType(ecsCloudMapService.ServiceType);
                            builder.WithCloudMapServiceInstanceId(associatedCloudMapServiceInstance.Id);

                            // Tags -> Labels

                            var cloudMapServiceLabels = BuildLabelsFromTags(
                                ecsCloudMapService.Tags,
                                discoveryResult.DiscoveryOptions.GetCloudMapServiceTagFilters(),
                                DiscoveryLabelPriority.CloudMapService
                            );
                            builder.WithLabels(cloudMapServiceLabels);

                            var cloudMapNamespaceLabels = BuildLabelsFromTags(
                                ecsCloudMapService.CloudMapNamespace.Tags,
                                discoveryResult.DiscoveryOptions.GetCloudMapNamespaceTagFilters(),
                                DiscoveryLabelPriority.CloudMapNamespace
                            );
                            builder.WithLabels(cloudMapNamespaceLabels);
                        }
                    }

                    // Tags -> Labels
                    var ecsTaskLabels = BuildLabelsFromTags(
                        ecsTask.Tags,
                        discoveryResult.DiscoveryOptions.GetEcsTaskTagFilters(),
                        DiscoveryLabelPriority.EcsTask
                    );
                    builder.WithLabels(ecsTaskLabels);

                    var ecsServiceLabels = BuildLabelsFromTags(
                        ecsService.Tags,
                        discoveryResult.DiscoveryOptions.GetEcsServiceTagFilters(),
                        DiscoveryLabelPriority.EcsService
                    );
                    builder.WithLabels(ecsServiceLabels);

                    // Now
                    var metricsPathPortMatcher = discoveryResult.DiscoveryOptions.MetricsPathPortTagPrefix;
                    // Translate to pattern
                    var metricsPathPattern = $"{metricsPathPortMatcher}PATH*".ConvertToRegexString();
                    var metricsPortPattern = $"{metricsPathPortMatcher}PORT*".ConvertToRegexString();

                    // Now match all labels that match the pattern
                    var metricsPathPortLabels = builder.Labels
                        .Where(label => label.Name.Match([metricsPathPattern, metricsPortPattern]))
                        .ToList();

                    // Now we need to group these in pairs of two,
                    // where the first label is the metrics path and the second is the port
                    // Note, that the path one may be missing,
                    // in which case we would be using "/metrics" as the default fallback
                    const string emptyGroupToken = "<empty>";
                    var metricsPathPortPairs = metricsPathPortLabels
                        .GroupBy(label => label.Name.Split('_').Last() switch
                        {
                            // If the trailing part is PATH or PORT,
                            // we will substitute it with the empty group token
                            "PATH" => emptyGroupToken,
                            "PORT" => emptyGroupToken,
                            _ => label.Name.Split('_').Last()
                        })
                        .Select(group => group.ToList())
                        .ToList();

                    // Extract the metrics path and port from the pairs into ScrapeConfigurations
                    var scrapeConfigurations = metricsPathPortPairs
                        .Select(pair =>
                        {
                            var path = pair.FirstOrDefault(label => label.Name.Contains("_PATH"))?.Value ?? "/metrics";
                            var port = ushort.Parse(pair.FirstOrDefault(label => label.Name.Contains("_PORT"))?.Value ?? "80");
                            return new ScrapeConfiguration
                            {
                                MetricsPath = path,
                                Port = port
                            };
                        })
                        .ToList();

                    // Add the scrape configurations
                    builder.WithScrapeConfigurations(scrapeConfigurations);

                    // Remove all such labels from the target
                    builder.WithoutLabels(metricsPathPortLabels);

                    discoveryTargets.Add(builder.Build());
                }
            }
        }

        return discoveryTargets;
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
    internal ICollection<DiscoveryLabel> BuildLabelsFromTags(
        ICollection<ResourceTag> resourceTags,
        ICollection<string> tagKeysFilters,
        DiscoveryLabelPriority priority)
    {
        if (tagKeysFilters.Count == 0) return [];
        var labels = resourceTags
            .Where(tag => PatternMatchingExtensions.Match(tag.Key, tagKeysFilters))
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