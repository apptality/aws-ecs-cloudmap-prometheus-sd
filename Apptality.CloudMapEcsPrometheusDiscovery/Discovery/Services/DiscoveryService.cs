using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components;
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

    public async Task<DiscoveryResult> Discover()
    {
        // Get ECS clusters
        var ecsClusters = await DiscoverEcsClusters();

        // Get CloudMap namespaces
        var cloudMapNamespaces = await DiscoverCloudMapNamespaces();

        // Build a complete list of CloudMap namespaces resources
        var cloudMapServiceConnectNamespacesResources = GetCompleteListOfCloudMapNamespaces(
            ecsClusters,
            cloudMapNamespaces
        );

        // In each CloudMap namespace, find all services that are registered
        // using AWS CloudMap Service Connect; this will be required later
        // to map ECS services to CloudMap services.
        // Note - we can only map Service Connect services this way,
        // but not Service Discovery ones (as those connections can only be inferred from tags)
        var ecsCloudMapServiceConnectServices = await Task.WhenAll(
            cloudMapNamespaces.Select(n => _ecsDiscovery.GetCloudMapServices(n.Arn))
        );

        // Build a complete list of ECS clusters Resources
        var allEcsClusterResources = GetCompleteListOfEcsClusters(
            ecsClusters,
            ecsCloudMapServiceConnectServices
        );

        // Get CloudMap services from each namespace
        var cloudMapServicesIds = cloudMapServiceConnectNamespacesResources.Select(s => s.Arn.Split('/')[1]);
        var cloudMapServices = (
                await Task.WhenAll(
                    cloudMapServicesIds.Select(namespaceId => _cloudMapServiceDiscovery.GetServices(namespaceId)))
            ).SelectMany(s => s) // Flatten the list of services
            .ToList();

        // Fetch tags for each CloudMap service
        await FetchCloudMapServicesTags(cloudMapServices);

        // Leave only those services that match the selector tags
        cloudMapServices = FilterCloudMapServicesBySelectorTags(cloudMapServices).ToList();

        // Fetch service instances for each CloudMap service
        await FetchCloudMapServicesInstances(cloudMapServices);

        // Identify CloudMap service types for each service
        IdentifyCloudMapServiceTypes(cloudMapServices);

        // Now that all services are fetched,
        // we can map CloudMap services to namespaces
        MapCloudMapServicesToNamespaces(cloudMapNamespaces, cloudMapServices);

        // Start resolving ECS services for each cluster concurrently
        var ecsClusterServices = await Task.WhenAll(
            allEcsClusterResources.Select(resource => _ecsDiscovery.GetClusterServices(resource.Arn))
        );

        // Get ECS services for each cluster
        var ecsClusterServicesArns = ecsClusterServices.SelectMany(c => c.ServiceArns).ToList();
        var ecsServices = await _ecsDiscovery.DescribeServices(ecsClusterServicesArns);

        // Leave only those services that match the selector tags
        ecsServices = FilterEcsServicesBySelectorTags(ecsServices).ToList();

        // Fetch running tasks for each ECS service
        await FetchEcsServicesRunningTasks(ecsServices);

        // Now we can map ECS services to clusters
        MapEcsServicesToClusters(ecsClusters, ecsServices);

        return new DiscoveryResult
        {
            DiscoveryOptions = _discoveryOptions,
            EcsClusters = ecsClusters,
            CloudMapNamespaces = cloudMapNamespaces
        };
    }

    /// <summary>
    /// Identifies CloudMap service types based on the service metadata
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to identify types for
    /// </param>
    private void IdentifyCloudMapServiceTypes(ICollection<CloudMapService> cloudMapServices)
    {
        foreach (var cloudMapService in cloudMapServices)
        {
            cloudMapService.ServiceType = CloudMapServiceTypeIdentifier.IdentifyServiceType(cloudMapService);
        }
    }

    /// <summary>
    /// Maps ECS services to their respective clusters
    /// </summary>
    internal void MapEcsServicesToClusters(ICollection<EcsCluster> ecsClusters, ICollection<EcsService> ecsServices)
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
    internal async Task FetchEcsServicesRunningTasks(ICollection<EcsService> ecsServices)
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
            cloudMapNamespace.ServiceSummaries = cloudMapServices
                .Where(s => s.NamespaceId == cloudMapNamespace.NamespaceSummary.Id)
                .ToList();
        }
    }

    /// <summary>
    /// For given CloudMap services, fetches instances for each service
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to fetch instances for
    /// </param>
    internal async Task FetchCloudMapServicesInstances(ICollection<CloudMapService> cloudMapServices)
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
    internal async Task<ICollection<CloudMapNamespace>> DiscoverCloudMapNamespaces()
    {
        // Get CloudMap namespaces from configuration
        var cloudMapNamespacesNames = _discoveryOptions.GetCloudMapNamespaceNames();

        if (cloudMapNamespacesNames.Length <= 0) return [];

        var cloudMapNamespacesSummaries = await _cloudMapServiceDiscovery.GetNamespaces(cloudMapNamespacesNames);
        if (cloudMapNamespacesSummaries.Count == 0)
        {
            _logger.LogDebug("No CloudMap namespaces found for the provided names: {@CloudMapNamespaces}",
                _discoveryOptions.CloudMapNamespaces);
        }

        // For each CloudMap namespace, get its tags
        var cloudMapNamespacesTags = await Task.WhenAll(
            cloudMapNamespacesSummaries.Select(ns => _cloudMapServiceDiscovery.GetTags(ns.Arn))
        );

        // Build a list of CloudMap namespaces with Tags
        var cloudMapNamespaces = new List<CloudMapNamespace>();

        foreach (var namespacesSummary in cloudMapNamespacesSummaries)
        {
            var sdns = new CloudMapNamespace
            {
                NamespaceSummary = namespacesSummary
            };
            cloudMapNamespaces.Add(sdns);

            var namespaceTags = cloudMapNamespacesTags.FirstOrDefault(t => t.ResourceArn == namespacesSummary.Arn);
            if (namespaceTags == null) continue;
            sdns.Tags = namespaceTags.Tags;
        }

        return cloudMapNamespaces;
    }

    /// <summary>
    /// Based on the application configuration, discovers ECS clusters
    /// </summary>
    /// <returns>
    /// Collection of ECS clusters
    /// </returns>
    internal async Task<ICollection<EcsCluster>> DiscoverEcsClusters()
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

    /// <summary>
    /// Builds a complete list of CloudMap namespaces by joining namespaces from ECS clusters and CloudMap
    /// </summary>
    /// <param name="ecsClusters">ECS clusters</param>
    /// <param name="cloudMapNamespaces">CloudMap namespaces</param>
    /// <returns>
    /// ARNs of all CloudMap namespaces that application needs to discover
    /// </returns>
    internal static ICollection<AwsResource> GetCompleteListOfCloudMapNamespaces(
        ICollection<EcsCluster> ecsClusters,
        ICollection<CloudMapNamespace> cloudMapNamespaces
    )
    {
        // Get CloudMap namespaces
        var ecsCloudMapServiceConnectNamespaces =
            ecsClusters.Select(c => c.Cluster.ServiceConnectDefaults.Namespace).ToList();
        var cloudMapServiceConnectNamespaces = cloudMapNamespaces.Select(n => n.Arn).ToList();
        // Join and deduplicate namespaces
        cloudMapServiceConnectNamespaces.AddRange(ecsCloudMapServiceConnectNamespaces);
        return cloudMapServiceConnectNamespaces.Distinct().Select(nsArn => new AwsResource(nsArn)).ToList();
    }

    /// <summary>
    /// Gets a complete list of ECS clusters by inferring them from CloudMap services,
    /// if no ECS clusters are found in the configuration
    /// </summary>
    /// <param name="ecsClusters">ECS clusters</param>
    /// <param name="ecsCloudMapServices">ECS CloudMap Registered Services</param>
    internal ICollection<AwsResource> GetCompleteListOfEcsClusters(
        ICollection<EcsCluster> ecsClusters,
        ICollection<EcsCloudMapServices> ecsCloudMapServices
    )
    {
        var allEcsClusterArns = ecsClusters
            .Select(c => c.ClusterArn)
            .ToList();

        // This means that no ECS clusters were found in the configuration;
        // thus we have to infer these from CloudMap services
        if (_discoveryOptions.GetEcsClustersNames().Length < 1)
        {
            // Now, infer ECS clusters ARNs from the discovered services
            var ecsClustersArnsInferredFromCloudMap = ecsCloudMapServices
                .SelectMany(s => s.ServiceArns)
                .Select(s =>
                {
                    var chunks = s.Split('/');
                    var clusterBaseArn = chunks[0].Replace(":service", ":cluster");
                    // Extract ECS cluster ARN
                    return $"{clusterBaseArn}/{chunks[1]}";
                })
                .Distinct();

            // Build and deduplicate a complete list of ECS clusters ARNs
            allEcsClusterArns.AddRange(ecsClustersArnsInferredFromCloudMap);
        }

        // Deduplicate and return
        return allEcsClusterArns.Distinct().Select(clusterArn => new AwsResource(clusterArn)).ToList();
    }

    /// <summary>
    /// Fetches tags for each CloudMap service and updates the service object
    /// </summary>
    /// <param name="cloudMapServices">
    /// Collection of CloudMap services to fetch tags for
    /// </param>
    internal async Task FetchCloudMapServicesTags(ICollection<CloudMapService> cloudMapServices)
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
    internal ICollection<EcsService> FilterEcsServicesBySelectorTags(ICollection<EcsService> ecsServices)
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