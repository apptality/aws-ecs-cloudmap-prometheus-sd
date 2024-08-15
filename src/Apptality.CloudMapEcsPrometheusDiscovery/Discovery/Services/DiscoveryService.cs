using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

/// <summary>
/// Service responsible for discovering ECS clusters and CloudMap namespaces
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly ILogger<DiscoveryService> _logger;
    private readonly IEcsDiscovery _ecsDiscovery;
    private readonly ICloudMapServiceDiscovery _cloudMapServiceDiscovery;
    private readonly DiscoveryOptions _discoveryOptions;

    // Default Ctor
    public DiscoveryService(
        ILogger<DiscoveryService> logger,
        IOptions<DiscoveryOptions> discoveryOptions,
        IEcsDiscovery ecsDiscovery,
        ICloudMapServiceDiscovery cloudMapServiceDiscovery
    )
    {
        _logger = logger;
        _discoveryOptions = discoveryOptions.Value;
        _ecsDiscovery = ecsDiscovery;
        _cloudMapServiceDiscovery = cloudMapServiceDiscovery;
    }

    /// <inheritdoc cref="IDiscoveryService.Discover"/>
    public async Task<DiscoveryResult> Discover()
    {
        // Get strategy for executing the discovery
        var discoveryExecutionStrategy = GetDiscoveryExecutionStrategy();

        // Execute the strategy
        return await discoveryExecutionStrategy();
    }

    /// <summary>
    /// Based on the provided settings, returns a strategy for executing the discovery
    /// of ECS clusters and CloudMap namespaces using the best possible approach
    /// that maximizes data completeness
    /// </summary>
    internal Func<Task<DiscoveryResult>> GetDiscoveryExecutionStrategy()
    {
        // Pre-check if both ECS clusters and CloudMap namespaces are provided
        var hasCloudMapNamespacesProvided = _discoveryOptions.HasCloudMapNamespaces();
        var hasEcsClustersProvided = _discoveryOptions.HasEcsClusters();

        // When both ECS cluster(s) and CloudMap namespace(s) provided via settings,
        // the result is the intersection of both:
        if (hasEcsClustersProvided && hasCloudMapNamespacesProvided)
        {
            return async () => new DiscoveryResult
            {
                DiscoveryOptions = _discoveryOptions,
                EcsClusters = await DiscoverEcsClusters(),
                CloudMapNamespaces = await DiscoverCloudMapNamespaces()
            };
        }

        // If only ECS cluster(s) provided, we can discover ECS clusters
        // and infer CloudMap namespaces from Service Connect configuration.
        // CloudMap "Service Discovery" services are not considered in this case,
        // as do not directly expose namespace association. If needed, they can be
        // included by providing "CloudMapNamespaces" via the configuration.
        if (hasEcsClustersProvided)
        {
            return async () =>
            {
                var ecsClusters = await DiscoverEcsClusters();

                var ecsServicesCloudMapServiceConnectNamespaces = ecsClusters
                    .SelectMany(c => c.Services)
                    .Select(s => s.Service.Deployments.FirstOrDefault(d => d.Status == "PRIMARY"))
                    .Where(s => s is {ServiceConnectConfiguration.Enabled: true})
                    .Select(s => s?.ServiceConnectConfiguration.Namespace.Split('/')[^1]!)
                    .Distinct()
                    .ToList();

                var cloudMapNamespaces = await DiscoverCloudMapNamespaces(
                    extraCloudMapNamespaces: ecsServicesCloudMapServiceConnectNamespaces
                );

                return new DiscoveryResult
                {
                    DiscoveryOptions = _discoveryOptions,
                    EcsClusters = ecsClusters,
                    CloudMapNamespaces = cloudMapNamespaces
                };
            };
        }

        // If only CloudMap namespace(s) provided, we can discover CloudMap namespaces
        // and infer ECS clusters from CloudMap services. In this case we have more flexibility
        // as we can infer ECS clusters from CloudMap services, both "Service Connect" and "Service Discovery".

        if (hasCloudMapNamespacesProvided)
        {
            return async () =>
            {
                var cloudMapNamespaces = await DiscoverCloudMapNamespaces();

                // TODO: Finish working on this part

                // var cloudMapEcsClusters = cloudMapNamespaces
                //     .SelectMany(n => n.ServiceSummaries)
                //     .Select(s => s.Service.Deployments.FirstOrDefault(d => d.Status == "PRIMARY"))
                //     .Where(s => s is {ServiceConnectConfiguration.Enabled: true})
                //     .Select(s => s?.ServiceConnectConfiguration.Namespace.Split('/')[^1]!)
                //     .Distinct()
                //     .ToList();

                var ecsClusters = await DiscoverEcsClusters(
                    // extraEcsClusters: cloudMapEcsClusters
                );

                return new DiscoveryResult
                {
                    DiscoveryOptions = _discoveryOptions,
                    EcsClusters = ecsClusters,
                    CloudMapNamespaces = cloudMapNamespaces
                };
            };
        }


        return () => Task.FromResult(new DiscoveryResult {DiscoveryOptions = _discoveryOptions});
    }

    /// <summary>
    /// Discovers information about CloudMap namespaces, services and instances
    /// </summary>
    internal async Task<ICollection<CloudMapNamespace>> DiscoverCloudMapNamespaces(
        ICollection<string>? extraCloudMapNamespaces = null
    )
    {
        // Get CloudMap namespaces
        var cloudMapNamespaces = await DiscoverCloudMapNamespacesFromSettings();

        // If extra namespaces are provided, fetch them
        if (extraCloudMapNamespaces is {Count: > 0})
        {
            await FetchMissingCloudMapNamespaces(cloudMapNamespaces, extraCloudMapNamespaces);
        }

        // Fetch CloudMap services for each CloudMap namespace
        var cloudMapServices = await FetchCloudMapServices(cloudMapNamespaces);

        // Fetch tags for each CloudMap service
        await FetchCloudMapServicesTags(cloudMapServices);

        // Leave only those services that match the selector tags
        cloudMapServices = FilterCloudMapServicesBySelectorTags(cloudMapServices).ToList();

        // Fetch service instances for each CloudMap service
        await FetchCloudMapServicesInstances(cloudMapServices);

        // Identify CloudMap service types for each service
        AssignCloudMapServiceTypes(cloudMapServices);

        // Map CloudMap services to namespaces
        MapCloudMapServicesToNamespaces(cloudMapNamespaces, cloudMapServices);

        return cloudMapNamespaces;
    }

    /// <summary>
    /// Discovers information about ECS clusters, services and tasks
    /// </summary>
    internal async Task<ICollection<EcsCluster>> DiscoverEcsClusters(
        ICollection<string>? extraEcsClusters = null
    )
    {
        // Get ECS clusters
        var ecsClusters = await DiscoverEcsClustersFromSettings();

        // If extra clusters are provided, fetch them
        if (extraEcsClusters is {Count: > 0})
        {
            await FetchMissingEcsClusters(ecsClusters, extraEcsClusters);
        }

        // Fetch ECS services for each ECS cluster
        var ecsServices = await FetchEcsClusterServices(ecsClusters);

        // Leave only those services that match the selector tags
        ecsServices = FilterEcsServicesBySelectorTags(ecsServices).ToList();

        // Fetch running tasks for each ECS service
        await FetchEcsServicesRunningTasks(ecsServices);

        // Map ECS services to clusters
        MapEcsServicesToClusters(ecsClusters, ecsServices);

        return ecsClusters;
    }

    /// <summary>
    /// Fetches ECS services for each ECS cluster
    /// </summary>
    /// <param name="ecsClusters">
    /// Collection of ECS clusters to fetch services for
    /// </param>
    /// <returns>
    /// Collection of ECS services
    /// </returns>
    internal async Task<ICollection<EcsService>> FetchEcsClusterServices(
        ICollection<EcsCluster> ecsClusters
    )
    {
        // Start resolving ECS services for each cluster concurrently
        var ecsClusterServices = await Task.WhenAll(
            ecsClusters.Select(resource => _ecsDiscovery.GetClusterServices(resource.ClusterArn))
        );
        // Get ECS services for each cluster
        var ecsClusterServicesArns = ecsClusterServices.SelectMany(c => c.ServiceArns).ToList();
        var ecsServices = await _ecsDiscovery.DescribeServices(ecsClusterServicesArns);

        return ecsServices;
    }

    /// <summary>
    /// Fetches CloudMap services for each CloudMap namespace
    /// </summary>
    /// <param name="cloudMapNamespaces">
    /// Collection of CloudMap namespaces to fetch services for
    /// </param>
    /// <returns>
    /// Collection of CloudMap services
    /// </returns>
    internal async Task<ICollection<CloudMapService>> FetchCloudMapServices(
        ICollection<CloudMapNamespace> cloudMapNamespaces
    )
    {
        // Get CloudMap services from each namespace
        var cloudMapServicesIds = cloudMapNamespaces.Select(s => s.Arn.Split('/')[1]);
        var cloudMapServices = await Task.WhenAll(
            cloudMapServicesIds.Select(namespaceId => _cloudMapServiceDiscovery.GetServices(namespaceId))
        );
        return cloudMapServices.SelectMany(s => s).ToList();
    }

    /// <summary>
    /// Compares the list of CloudMap namespaces with the list of all CloudMap namespace resources,
    /// and fetches the missing CloudMap namespaces
    /// </summary>
    /// <param name="cloudMapNamespaces">CloudMap Namespaces already discovered</param>
    /// <param name="ecsServiceConnectNamespaces">Additional CloudMap namespaces Ids inferred from ECS CloudMap integration</param>
    internal async Task FetchMissingCloudMapNamespaces(
        ICollection<CloudMapNamespace> cloudMapNamespaces,
        ICollection<string> ecsServiceConnectNamespaces
    )
    {
        // If we inferred CloudMap namespaces from ECS services, we need to fetch them
        var missingCloudMapNamespaces = ecsServiceConnectNamespaces
            .Where(r => cloudMapNamespaces.All(c => c.Id != r))
            .ToList();

        if (missingCloudMapNamespaces.Count > 0)
        {
            var missingNamespaces = await _cloudMapServiceDiscovery.GetNamespacesByIds(missingCloudMapNamespaces);
            foreach (var missingNamespace in missingNamespaces)
            {
                cloudMapNamespaces.Add(missingNamespace);
            }
        }
    }

    /// <summary>
    /// Compares the list of ECS clusters with the list of all ECS cluster resources,
    /// and fetches the missing ECS clusters
    /// </summary>
    /// <param name="ecsClusters">ECS Clusters Already discovered</param>
    /// <param name="cloudMapEcsClusters">Additional ECS Clusters inferred from CloudMap resources</param>
    internal async Task FetchMissingEcsClusters(
        ICollection<EcsCluster> ecsClusters,
        ICollection<string> cloudMapEcsClusters
    )
    {
        // If we inferred ECS clusters from CloudMap services, we need to fetch them
        var missingEcsClusters = cloudMapEcsClusters
            .Where(r => ecsClusters.All(c => c.ClusterName != r))
            .ToList();

        if (missingEcsClusters.Count > 0)
        {
            var missingEcsClustersData = await _ecsDiscovery.GetClusters(missingEcsClusters);
            foreach (var missingEcsCluster in missingEcsClustersData)
            {
                ecsClusters.Add(missingEcsCluster);
            }
        }
    }

    /// <summary>
    /// Assigns CloudMap service types based on the service metadata
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to identify types for
    /// </param>
    internal void AssignCloudMapServiceTypes(
        ICollection<CloudMapService> cloudMapServices
    )
    {
        foreach (var cloudMapService in cloudMapServices)
        {
            cloudMapService.ServiceType = CloudMapServiceTypeIdentifier.IdentifyServiceType(cloudMapService);
        }
    }

    /// <summary>
    /// Maps ECS services to their respective clusters
    /// </summary>
    internal void MapEcsServicesToClusters(
        ICollection<EcsCluster> ecsClusters,
        ICollection<EcsService> ecsServices
    )
    {
        // Map ECS services to clusters
        foreach (var ecsCluster in ecsClusters)
        {
            ecsCluster.Services = ecsServices.Where(s => s.ClusterArn == ecsCluster.ClusterArn).ToList();
        }
    }

    /// <summary>
    /// Fetches running tasks for each ECS service and updates the service object
    /// </summary>
    /// <param name="ecsServices">
    /// ECS Services to fetch running tasks for
    /// </param>
    internal async Task FetchEcsServicesRunningTasks(
        ICollection<EcsService> ecsServices
    )
    {
        // Now we need to describe running tasks
        // First, group all service ARNs by cluster ARN
        var ecsClusterServicesGroups = ecsServices.Select(s => new {s.ServiceArn, s.ClusterArn}).ToList();

        // This is an ultimate linking point between cluster, service and running task
        var ecsRunningTasksArns = await Task.WhenAll(
            ecsClusterServicesGroups.Select(s => _ecsDiscovery.GetRunningTasks(s.ClusterArn, s.ServiceArn))
        );

        // Describe all running tasks
        var ecsRunningTasksFutures = ecsRunningTasksArns.Select(s =>
            _ecsDiscovery.DescribeTasks(s.ClusterArn, s.ServiceArn, s.RunningTaskArns));

        var ecsRunningTasks = (await Task.WhenAll(ecsRunningTasksFutures))
            .SelectMany(t => t) // Flatten the list of ECS Tasks
            .ToList();

        // Now map running tasks to services
        foreach (var ecsService in ecsServices)
        {
            ecsService.Tasks = ecsRunningTasks.Where(t => t.ServiceArn == ecsService.ServiceArn).ToList();
        }
    }

    /// <summary>
    /// Maps CloudMap services to their respective namespaces
    /// </summary>
    internal static void MapCloudMapServicesToNamespaces(
        ICollection<CloudMapNamespace> cloudMapNamespaces,
        ICollection<CloudMapService> cloudMapServices)
    {
        foreach (var cloudMapNamespace in cloudMapNamespaces)
        {
            var relatedServiceSummaries = cloudMapServices
                .Where(s => s.NamespaceId == cloudMapNamespace.NamespaceSummary.Id)
                .ToList();
            cloudMapNamespace.SetServiceSummaries(relatedServiceSummaries);
        }
    }

    /// <summary>
    /// For given CloudMap services, fetches instances for each service
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to fetch instances for
    /// </param>
    internal async Task FetchCloudMapServicesInstances(
        ICollection<CloudMapService> cloudMapServices
    )
    {
        // Get CloudMap instances for each service
        var cloudMapServiceInstances = (await Task.WhenAll(
            cloudMapServices.Select(s => _cloudMapServiceDiscovery.GetServiceInstances(s.ServiceSummary.Id))
        )).SelectMany(s => s).ToList();

        // Update service summary with instances summaries
        foreach (var cloudMapService in cloudMapServices)
        {
            cloudMapService.InstanceSummaries = cloudMapServiceInstances
                .Where(i => i.ServiceId == cloudMapService.ServiceSummary.Id)
                .ToList();
        }
    }

    /// <summary>
    /// Based on the application configuration, discovers CloudMap namespaces
    /// </summary>
    /// <returns>
    /// Collection of CloudMap namespaces
    /// </returns>
    internal async Task<ICollection<CloudMapNamespace>> DiscoverCloudMapNamespacesFromSettings()
    {
        // Get CloudMap namespaces from configuration
        var cloudMapNamespacesNames = _discoveryOptions.GetCloudMapNamespaceNames();

        if (cloudMapNamespacesNames.Length <= 0) return [];

        var cloudMapNamespaces = await _cloudMapServiceDiscovery.GetNamespacesByNames(cloudMapNamespacesNames);
        if (cloudMapNamespaces.Count == 0)
        {
            _logger.LogDebug("No CloudMap namespaces found for the provided names: {@CloudMapNamespaces}",
                _discoveryOptions.CloudMapNamespaces);
        }

        // For each CloudMap namespace, get its tags
        var cloudMapNamespacesTags = await Task.WhenAll(
            cloudMapNamespaces.Select(ns => _cloudMapServiceDiscovery.GetTags(ns.Arn))
        );

        foreach (var cloudMapNamespace in cloudMapNamespaces)
        {
            var namespaceTags = cloudMapNamespacesTags.FirstOrDefault(t => t.ResourceArn == cloudMapNamespace.Arn);
            if (namespaceTags == null) continue;
            cloudMapNamespace.Tags = namespaceTags.Tags;
        }

        return cloudMapNamespaces;
    }

    /// <summary>
    /// Based on the application configuration, discovers ECS clusters
    /// </summary>
    /// <returns>
    /// Collection of ECS clusters
    /// </returns>
    internal async Task<ICollection<EcsCluster>> DiscoverEcsClustersFromSettings()
    {
        var ecsClustersNames = _discoveryOptions.GetEcsClustersNames();
        if (ecsClustersNames.Length <= 0) return [];

        var ecsClusters = await _ecsDiscovery.GetClusters(ecsClustersNames);
        if (ecsClusters.Count == 0)
        {
            _logger.LogDebug("No ECS clusters found for the provided names: {@EcsClustersNames}", ecsClustersNames);
        }

        return ecsClusters;
    }

    // /// <summary>
    // /// Builds a complete list of CloudMap namespaces by joining namespaces from ECS clusters and CloudMap
    // /// </summary>
    // /// <param name="ecsClusters">ECS clusters</param>
    // /// <param name="cloudMapNamespaces">CloudMap namespaces</param>
    // /// <returns>
    // /// ARNs of all CloudMap namespaces that application needs to discover
    // /// </returns>
    // internal static ICollection<AwsResource> GetCompleteListOfCloudMapNamespaces(
    //     ICollection<EcsCluster> ecsClusters,
    //     ICollection<CloudMapNamespace> cloudMapNamespaces
    // )
    // {
    //     // Get CloudMap namespaces
    //     var ecsCloudMapServiceConnectNamespaces =
    //         ecsClusters.Select(c => c.Cluster.ServiceConnectDefaults?.Namespace).ToList();
    //     var cloudMapServiceConnectNamespaces = cloudMapNamespaces.Select(n => n.Arn).ToList();
    //     // Join and deduplicate namespaces
    //     cloudMapServiceConnectNamespaces.AddRange(
    //         ecsCloudMapServiceConnectNamespaces.Where(s => !string.IsNullOrEmpty(s))!);
    //     return cloudMapServiceConnectNamespaces.Distinct().Select(nsArn => new AwsResource(nsArn)).ToList();
    // }

    // /// <summary>
    // /// Gets a complete list of ECS clusters by inferring them from CloudMap services,
    // /// if no ECS clusters are found in the configuration
    // /// </summary>
    // /// <param name="ecsClusters">ECS clusters</param>
    // /// <param name="ecsCloudMapServices">ECS CloudMap Registered Services</param>
    // internal ICollection<AwsResource> GetCompleteListOfEcsClusters(
    //     ICollection<EcsCluster> ecsClusters,
    //     ICollection<EcsCloudMapServices> ecsCloudMapServices
    // )
    // {
    //     var allEcsClusterArns = ecsClusters
    //         .Select(c => c.ClusterArn)
    //         .ToList();
    //
    //     // This means that no ECS clusters were found in the configuration;
    //     // thus we have to infer these from CloudMap services
    //     if (_discoveryOptions.GetEcsClustersNames().Length < 1)
    //     {
    //         // Now, infer ECS clusters ARNs from the discovered services
    //         var ecsClustersArnsInferredFromCloudMap = ecsCloudMapServices
    //             .SelectMany(s => s.ServiceArns)
    //             .Select(s =>
    //             {
    //                 var chunks = s.Split('/');
    //                 var clusterBaseArn = chunks[0].Replace(":service", ":cluster");
    //                 // Extract ECS cluster ARN
    //                 return $"{clusterBaseArn}/{chunks[1]}";
    //             })
    //             .Distinct();
    //
    //         // Build and deduplicate a complete list of ECS clusters ARNs
    //         allEcsClusterArns.AddRange(ecsClustersArnsInferredFromCloudMap);
    //     }
    //
    //     // Deduplicate and return
    //     return allEcsClusterArns.Distinct().Select(clusterArn => new AwsResource(clusterArn)).ToList();
    // }

    /// <summary>
    /// Fetches tags for each CloudMap service and updates the service object
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to fetch tags for
    /// </param>
    internal async Task FetchCloudMapServicesTags(
        ICollection<CloudMapService> cloudMapServices
    )
    {
        // For each CloudMap service, get its tags
        var cloudMapServicesTags = await Task.WhenAll(
            cloudMapServices.Select(ss => _cloudMapServiceDiscovery.GetTags(ss.ServiceSummary.Arn))
        );

        // Fetch tags for each service
        foreach (var cloudMapServicesTag in cloudMapServicesTags)
        {
            var service = cloudMapServices.FirstOrDefault(
                s => s.ServiceSummary.Arn == cloudMapServicesTag.ResourceArn
            );
            if (service == null) continue;
            service.Tags = cloudMapServicesTag.Tags;
        }
    }

    /// <summary>
    /// Filters CloudMap services by selector tags provided via configuration
    /// </summary>
    /// <param name="cloudMapServices">
    /// Services to be filtered
    /// </param>
    /// <returns>
    /// New collection of services that match the selector tags
    /// </returns>
    internal ICollection<CloudMapService> FilterCloudMapServicesBySelectorTags(
        ICollection<CloudMapService> cloudMapServices
    )
    {
        // Create a filter
        var cloudMapServiceSelectorTags = _discoveryOptions.GetCloudMapServiceSelectorTags();
        var cloudMapServiceResourceTagFilter = new ResourceTagSelectorFilter(cloudMapServiceSelectorTags);
        // Apply filter
        return cloudMapServices.Where(
            s => cloudMapServiceResourceTagFilter.Matches(s.Tags)
        ).ToList();
    }

    /// <summary>
    /// Filters ECS services by selector tags provided via configuration
    /// </summary>
    /// <param name="ecsServices">
    /// Services to be filtered
    /// </param>
    /// <returns>
    /// New collection of services that match the selector tags
    /// </returns>
    internal ICollection<EcsService> FilterEcsServicesBySelectorTags(
        ICollection<EcsService> ecsServices
    )
    {
        // Create a filter
        var ecsServiceSelectorTags = _discoveryOptions.GetEcsServiceSelectorTags();
        var ecsServiceResourceTagFilter = new ResourceTagSelectorFilter(ecsServiceSelectorTags);

        // Translate resource to a filterable object
        var ecsServiceResourcesWithTags = ecsServices
            .Select(c => Resource.FromObject(
                c,
                (s) => s.ServiceArn,
                (s) => s.Service.Tags.Select(t => new ResourceTag {Key = t.Key, Value = t.Value}).ToList()
            )).ToList();

        // Apply filter
        var filteredEcsServiceResourcesArns = ecsServiceResourcesWithTags
            .Where(r => ecsServiceResourceTagFilter.Matches(r.Tags))
            .Select(s => s.Arn)
            .ToList();

        // Return filtered services
        return ecsServices
            .Where(s => filteredEcsServiceResourcesArns.Contains(s.ServiceArn))
            .ToList();
    }
}