using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Factories;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;

/// <summary>
/// Concrete implementation of <see cref="ICloudMapServiceDiscovery"/> interface
/// </summary>
public class CloudMapServiceDiscovery(
    ILogger<CloudMapServiceDiscovery> logger,
    IAmazonServiceDiscovery serviceDiscoveryClient
) : ICloudMapServiceDiscovery
{
    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetNamespacesByNames"/>
    /// <remarks>
    /// Read more about the ListNamespaces API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListNamespacesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapNamespace>> GetNamespacesByNames(ICollection<string> namespaceNames)
    {
        // From namespaceNames filter out those that are ARNs
        // var namespaceNamesArns = namespaceNames

        var namespaceFilters = NamespaceFilterFactory.Create(namespaceNames);
        // If no filters are provided, return null
        if (namespaceFilters.Count == 0) return [];

        var namespaces = new List<NamespaceSummary>();

        // Iterate over all namespace filters and fetch data for each.
        // This is also a pretty slow operation
        foreach (var namespaceFilter in namespaceFilters)
        {
            var request = new ListNamespacesRequest { Filters = [namespaceFilter] };
            logger.LogDebug("Fetching data for the following namespaces: {@Namespaces}", request.Filters);

            // Fetch details on all namespaces specified in the filters
            var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
                request,
                async req => await serviceDiscoveryClient.ListNamespacesAsync(req),
                response => response.NextToken,
                (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
            );

            // Merge all responses into a single response
            var requestedNamespaces = responses.SelectMany(response => response.Namespaces).ToList();
            namespaces.AddRange(requestedNamespaces);
            logger.LogDebug("Fetched data for the following namespace {Namespaces}: {@Namespaces}", namespaceFilter.Values.First(), requestedNamespaces);
        }

        return namespaces.Select(ns => new CloudMapNamespace
        {
            NamespaceSummary = ns
        }).ToList();
    }

    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetNamespacesByIds"/>
    /// <remarks>
    /// Read more about the ListNamespaces API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TGetNamespaceRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapNamespace>> GetNamespacesByIds(ICollection<string> namespaceIds)
    {
        var namespaceRequests = namespaceIds
            .Where(ns => ns.StartsWith("ns-")) // Filter out anything that doesn't start with 'ns-'
            .Select(nsId => serviceDiscoveryClient.GetNamespaceAsync(new GetNamespaceRequest { Id = nsId }));

        logger.LogDebug("Fetching data for the following namespaces: {@NamespaceIds}", namespaceIds);

        // Wait for all requests to complete
        var namespaces = await Task.WhenAll(namespaceRequests);

        // Return the namespaces converted to CloudMapNamespace
        var cloudMapNamespaces = namespaces.Select(ns => new CloudMapNamespace
        {
            NamespaceSummary = new NamespaceSummary
            {
                Arn = ns.Namespace.Arn,
                CreateDate = ns.Namespace.CreateDate,
                Description = ns.Namespace.Description,
                Id = ns.Namespace.Id,
                Name = ns.Namespace.Name,
                Properties = ns.Namespace.Properties,
                ServiceCount = ns.Namespace.ServiceCount,
                Type = ns.Namespace.Type
            }
        }).ToList();

        logger.LogDebug("Fetched data for the following namespaces: {@Namespaces}", cloudMapNamespaces);

        return cloudMapNamespaces;
    }

    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetServices"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListServicesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapService>> GetServices(string namespaceId)
    {
        var serviceFilters = ServiceFilterFactory.Create(namespaceId);
        var request = new ListServicesRequest { Filters = serviceFilters };

        logger.LogDebug("Fetching services for namespace: {NamespaceId}", namespaceId);

        // If no filters are provided, return null
        if (request.Filters.Count == 0) return [];

        var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
            request,
            async req => await serviceDiscoveryClient.ListServicesAsync(req),
            response => response.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken);

        // Merge all responses into a single response
        var cloudMapServices = responses
            .SelectMany(response => response.Services)
            .Select(serviceSummary => new CloudMapService
            {
                NamespaceId = namespaceId,
                ServiceSummary = serviceSummary
            })
            .ToList();

        logger.LogDebug("Fetched services for namespace {NamespaceId}: {@CloudMapServices}", namespaceId,
            cloudMapServices);

        return cloudMapServices;
    }

    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetServiceInstances"/>
    /// <remarks>
    /// Read more about the ListInstances API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListInstancesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapServiceInstance>> GetServiceInstances(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));
        }

        logger.LogDebug("Fetching instances for service: {ServiceId}", serviceId);

        var request = new ListInstancesRequest { ServiceId = serviceId };

        var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
            request,
            async req => await serviceDiscoveryClient.ListInstancesAsync(req),
            response => response.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken);

        // Merge all responses into a single response
        var serviceInstances = responses
            .SelectMany(response => response.Instances)
            .Select(instanceSummary => new CloudMapServiceInstance
            {
                ServiceId = serviceId,
                InstanceSummary = instanceSummary
            })
            .ToList();

        logger.LogDebug("Fetched instances for service {ServiceId}: {@ServiceInstances}", serviceId, serviceInstances);

        return serviceInstances;
    }

    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetTags"/>
    /// <remarks>
    /// Read more about the ListTagsForResource API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListTagsForResourceRequest.html">here</a>
    /// </remarks>
    public async Task<CloudMapResourceTags> GetTags(string resourceArn)
    {
        if (string.IsNullOrWhiteSpace(resourceArn))
        {
            throw new ArgumentException("Resource ARN cannot be null or empty", nameof(resourceArn));
        }

        var request = new ListTagsForResourceRequest { ResourceARN = resourceArn };
        ListTagsForResourceResponse response;

        try
        {
            response = await serviceDiscoveryClient.ExecuteWithRetryAsync(async () =>
                await serviceDiscoveryClient.ListTagsForResourceAsync(request));
        }
        catch (ResourceNotFoundException)
        {
            return new CloudMapResourceTags(resourceArn, []);
        }

        return new CloudMapResourceTags(resourceArn,
            response.Tags.Select(t => new ResourceTag { Key = t.Key, Value = t.Value }).ToArray()
        );
    }
}