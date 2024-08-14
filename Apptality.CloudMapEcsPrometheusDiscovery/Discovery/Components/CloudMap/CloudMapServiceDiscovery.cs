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
    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetNamespaces"/>
    /// <remarks>
    /// Read more about the ListNamespaces API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListNamespacesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapNamespace>> GetNamespaces(ICollection<string> namespaceNames)
    {
        var namespaceFilters = NamespaceFilterFactory.Create(namespaceNames);
        var request = new ListNamespacesRequest {Filters = namespaceFilters};
        logger.LogDebug("Fetching data for the following namespaces: {@Namespaces}", request.Filters);

        // If no filters are provided, return null
        if (request.Filters.Count == 0) return [];

        // Fetch details on all namespaces specified in the filters
        var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
            request,
            async req => await serviceDiscoveryClient.ListNamespacesAsync(req),
            response => response.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        // Merge all responses into a single response
        var namespaces = responses.SelectMany(response => response.Namespaces).ToList();
        logger.LogDebug("Fetched data for the following namespaces: {@Namespaces}", namespaces);

        return namespaces.Select(ns => new CloudMapNamespace
        {
            NamespaceSummary = ns
        }).ToList();
    }

    /// <inheritdoc cref="ICloudMapServiceDiscovery.GetServices"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TListServicesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<CloudMapService>> GetServices(string namespaceId)
    {
        var serviceFilters = ServiceFilterFactory.Create(namespaceId);
        var request = new ListServicesRequest {Filters = serviceFilters};

        // If no filters are provided, return null
        if (request.Filters.Count == 0) return [];

        var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
            request,
            async req => await serviceDiscoveryClient.ListServicesAsync(req),
            response => response.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken);

        // Merge all responses into a single response
        return responses
            .SelectMany(response => response.Services)
            .Select(serviceSummary => new CloudMapService
            {
                NamespaceId = namespaceId,
                ServiceSummary = serviceSummary
            })
            .ToList();
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

        var request = new ListInstancesRequest {ServiceId = serviceId};

        var responses = await serviceDiscoveryClient.FetchAllPagesAsync(
            request,
            async req => await serviceDiscoveryClient.ListInstancesAsync(req),
            response => response.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken);

        // Merge all responses into a single response
        return responses
            .SelectMany(response => response.Instances)
            .Select(instanceSummary => new CloudMapServiceInstance
            {
                ServiceId = serviceId,
                InstanceSummary = instanceSummary
            })
            .ToList();
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

        var request = new ListTagsForResourceRequest {ResourceARN = resourceArn};
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
            response.Tags.Select(t => new ResourceTag {Key = t.Key, Value = t.Value}).ToArray()
        );
    }
}