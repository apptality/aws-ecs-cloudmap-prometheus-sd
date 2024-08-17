using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services.Internals;

/// <summary>
/// Service that performs discovery of CloudMap namespaces,
/// services and instances and constructs a tree-like
/// model of the discovered resources
/// </summary>
public class CloudMapDiscoveryService : ICloudMapDiscoveryService
{
    private readonly ILogger<CloudMapDiscoveryService> _logger;
    private readonly ICloudMapServiceDiscovery _cloudMapServiceDiscovery;
    private readonly DiscoveryOptions _discoveryOptions;
    private readonly IMemoryCache _memoryCache;

    public CloudMapDiscoveryService(
        ILogger<CloudMapDiscoveryService> logger,
        ICloudMapServiceDiscovery cloudMapServiceDiscovery,
        IOptions<DiscoveryOptions> discoveryOptions,
        IMemoryCache memoryCache
    )
    {
        _logger = logger;
        _cloudMapServiceDiscovery = cloudMapServiceDiscovery;
        _discoveryOptions = discoveryOptions.Value;
        _memoryCache = memoryCache;
    }

    /// <see cref="ICloudMapDiscoveryService.DiscoverCloudMapResources"/>
    public async Task<ICollection<CloudMapNamespace>> DiscoverCloudMapResources(
        ICollection<string>? extraCloudMapNamespaces = null
    )
    {
        // Discover CloudMap namespaces
        var cloudMapNamespaces = await GetCloudMapNamespaces(extraCloudMapNamespaces);

        // Fetch CloudMap services for each CloudMap namespace
        var cloudMapServices = await FetchCloudMapServices(cloudMapNamespaces);

        // Fetch tags for each CloudMap service
        await FetchCloudMapServicesTags(cloudMapServices);

        // Leave only those services that match the selector tags
        cloudMapServices = FilterCloudMapServicesBySelectorTags(cloudMapServices);

        // Fetch service instances for each CloudMap service
        await FetchCloudMapServicesInstances(cloudMapServices);

        // Identify CloudMap service types for each service
        AssignCloudMapServiceTypes(cloudMapServices);

        // Map CloudMap services to namespaces
        MapCloudMapServicesToNamespaces(cloudMapNamespaces, cloudMapServices);

        return cloudMapNamespaces;
    }

    /// <summary>
    /// Depending on the caching settings, fetches CloudMap namespaces
    /// from cache or directly from AWS
    /// </summary>
    /// <param name="extraCloudMapNamespaces">
    /// Extra CloudMap namespaces to fetch
    /// in addition to the ones already supplied in the settings
    /// </param>
    internal async Task<ICollection<CloudMapNamespace>> GetCloudMapNamespaces(
        ICollection<string>? extraCloudMapNamespaces = null
    )
    {
        // If no caching is enabled, fetch CloudMap namespaces and return
        if (!_discoveryOptions.CacheTopLevelResources)
        {
            _logger.LogDebug("Fetching CloudMap namespaces without caching");
            return await FetchCloudMapNamespaces(extraCloudMapNamespaces);
        }

        // If caching is enabled, try to get CloudMap namespaces from cache
        var cacheKey = $"CloudMapNamespaces-{extraCloudMapNamespaces.ComputeHash()}";
        var cacheDuration = TimeSpan.FromMinutes(10);
        if (_memoryCache.TryGetValue(cacheKey, out ICollection<CloudMapNamespace>? cloudMapNamespaces))
        {
            // Don't cache empty results
            if (cloudMapNamespaces is {Count: > 0})
            {
                _logger.LogDebug("CloudMap namespaces found in cache");
                return cloudMapNamespaces;
            }
        }

        // If not found in cache, fetch CloudMap namespaces and cache them
        cloudMapNamespaces = await FetchCloudMapNamespaces(extraCloudMapNamespaces);
        _memoryCache.Set(cacheKey, cloudMapNamespaces, cacheDuration);
        _logger.LogDebug("CloudMap namespaces fetched and cached");
        return cloudMapNamespaces;
    }

    /// <summary>
    /// Fetches CloudMap namespaces from AWS
    /// </summary>
    /// <param name="extraCloudMapNamespaces">
    /// Extra CloudMap namespaces to fetch
    /// in addition to the ones already supplied in the settings
    /// </param>
    internal async Task<ICollection<CloudMapNamespace>> FetchCloudMapNamespaces(
        ICollection<string>? extraCloudMapNamespaces = null)
    {
        // Get CloudMap namespaces
        var namespaces = await DiscoverCloudMapNamespacesFromSettings();

        // If extra namespaces are provided, fetch them
        if (extraCloudMapNamespaces is {Count: > 0})
        {
            await FetchMissingCloudMapNamespaces(namespaces, extraCloudMapNamespaces);
        }

        return namespaces;
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
    internal List<CloudMapService> FilterCloudMapServicesBySelectorTags(
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
}