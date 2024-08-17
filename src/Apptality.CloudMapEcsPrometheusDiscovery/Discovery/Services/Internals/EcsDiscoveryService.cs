using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services.Internals;

/// <summary>
/// Service that performs ECS discovery of ECS clusters,
/// services and running tasks and constructs a tree-like
/// model of the discovered resources
/// </summary>
public class EcsDiscoveryService : IEcsDiscoveryService
{
    private readonly DiscoveryOptions _discoveryOptions;
    private readonly ILogger<EcsDiscoveryService> _logger;
    private readonly IEcsDiscovery _ecsDiscovery;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Service that performs ECS discovery of ECS clusters,
    /// services and running tasks and constructs a tree-like
    /// model of the discovered resources
    /// </summary>
    public EcsDiscoveryService(
        ILogger<EcsDiscoveryService> logger,
        IEcsDiscovery ecsDiscovery,
        IOptions<DiscoveryOptions> discoveryOptions,
        IMemoryCache memoryCache
    )
    {
        _logger = logger;
        _ecsDiscovery = ecsDiscovery;
        _discoveryOptions = discoveryOptions.Value;
        _memoryCache = memoryCache;
    }

    /// <see cref="IEcsDiscoveryService.DiscoverEcsResources"/>
    public async Task<ICollection<EcsCluster>> DiscoverEcsResources(
        ICollection<string>? extraEcsClusters = null
    )
    {
        // Discover ECS clusters
        var ecsClusters = await DiscoverEcsClusters(extraEcsClusters);

        // Fetch ECS services for each ECS cluster
        var ecsServices = await FetchEcsClusterServices(ecsClusters);

        // Leave only those services that match the selector tags
        ecsServices = FilterEcsServicesBySelectorTags(ecsServices);

        // Fetch running tasks for each ECS service
        await FetchEcsServicesRunningTasks(ecsServices);

        // Map ECS services to clusters
        MapEcsServicesToClusters(ecsClusters, ecsServices);

        return ecsClusters;
    }

    /// <summary>
    /// Depending on the caching settings, fetches ECS clusters
    /// from cache or directly from AWS
    /// </summary>
    /// <param name="extraEcsClusters">
    /// Extra ECS clusters to fetch
    /// in addition to the ones already supplied in the settings
    /// </param>
    internal async Task<ICollection<EcsCluster>> DiscoverEcsClusters(
        ICollection<string>? extraEcsClusters = null
    )
    {
        if (!_discoveryOptions.CacheTopLevelResources)
        {
            _logger.LogDebug("Fetching ECS clusters without caching");
            return await FetchEcsClusters(extraEcsClusters);
        }

        var cacheKey = $"EcsClusters-{extraEcsClusters.ComputeHash()}";
        var cacheDuration = TimeSpan.FromMinutes(10);
        if (_memoryCache.TryGetValue(cacheKey, out ICollection<EcsCluster>? ecsClusters))
        {
            // Don't return the cached value if extra clusters are provided
            if (ecsClusters is {Count: > 0})
            {
                _logger.LogDebug("ECS clusters found in cache");
                return ecsClusters;
            }
        }

        ecsClusters = await FetchEcsClusters(extraEcsClusters);
        _memoryCache.Set(cacheKey, ecsClusters, cacheDuration);
        _logger.LogDebug("ECS clusters fetched and cached");
        return ecsClusters;
    }

    /// <summary>
    /// Fetches ECS Clusters from AWS
    /// </summary>
    /// <param name="extraEcsClusters">
    /// Extra ECS Clusters to fetch
    /// in addition to the ones already supplied in the settings
    /// </param>
    internal async Task<ICollection<EcsCluster>> FetchEcsClusters(
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

    /// <summary>
    /// Filters ECS services by selector tags provided via configuration
    /// </summary>
    /// <param name="ecsServices">
    /// Services to be filtered
    /// </param>
    /// <returns>
    /// New collection of services that match the selector tags
    /// </returns>
    internal List<EcsService> FilterEcsServicesBySelectorTags(
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