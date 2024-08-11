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
    public async Task<ICollection<NamespaceSummary>> GetNamespaces(ICollection<string> namespaceNames)
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

        return namespaces;

        // TODO: Remove
        // Response example structure:
        // [
        //     {
        //         "arn": "arn:aws:servicediscovery:us-west-2:123456789012:namespace/ns-ke2ewbfvynxeu2ub",
        //         "createDate": "2024-05-01T22:28:50.6689999Z",
        //         "description": "Private DNS namespace for ECS services to communicate with each other",
        //         "id": "ns-ke2ewbfvynxeu2ub",
        //         "name": "cloudmap.namespace",
        //         "properties": {
        //             "dnsProperties": {
        //                 "hostedZoneId": "Z09056732A484XV4MXE32",
        //                 "soa": {
        //                     "ttl": 15
        //                 }
        //             },
        //             "httpProperties": {
        //                 "httpName": "cloudmap.namespace"
        //             }
        //         },
        //         "serviceCount": 0,
        //         "type": {
        //             "value": "DNS_PRIVATE"
        //         }
        //     },
        // ]
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

        // TODO: Remove
        // [
        //     {
        //       "arn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-5kgcrz424wxiejsv",
        //       "createDate": "2024-07-13T05:23:11.5669999Z",
        //       "description": "Managed by arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-http",
        //       "dnsConfig": {
        //         "dnsRecords": [],
        //         "namespaceId": null,
        //         "routingPolicy": null
        //       },
        //       "healthCheckConfig": null,
        //       "healthCheckCustomConfig": null,
        //       "id": "srv-5kgcrz424wxiejsv",
        //       "instanceCount": 0,
        //       "name": "service-http",
        //       "type": {
        //         "value": "HTTP"
        //       }
        //     },
        //     {
        //       "arn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-zo6epblsayj3h5za",
        //       "createDate": "2024-07-16T08:22:58.1889998Z",
        //       "description": "Service Discovery for Prometheus metrics",
        //       "dnsConfig": {
        //         "dnsRecords": [
        //           {
        //             "ttl": 10,
        //             "type": {
        //               "value": "A"
        //             }
        //           }
        //         ],
        //         "namespaceId": null,
        //         "routingPolicy": {
        //           "value": "MULTIVALUE"
        //         }
        //       },
        //       "healthCheckConfig": null,
        //       "healthCheckCustomConfig": {
        //         "failureThreshold": 1
        //       },
        //       "id": "srv-zo6epblsayj3h5za",
        //       "instanceCount": 0,
        //       "name": "metrics-sd-service-http",
        //       "type": {
        //         "value": "DNS_HTTP"
        //       }
        //     },
        //   ]
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

        // TODO: Remove
        // service-http (cloud map instances)
        // [
        //     {
        //         "attributes": {
        //             "AWS_INSTANCE_IPV4": "10.200.65.58",
        //             "AWS_INSTANCE_PORT": "8080",
        //             "AvailabilityZone": "us-west-2a",
        //             "DeploymentId": "arn:aws:ecs:us-west-2:123456789012:task-set/ecs-cluster-name/service-http/ecs-svc/2511542921236936597",
        //             "IdleTimeoutInSeconds": "0",
        //             "PerRequestTimeoutInSeconds": "0"
        //         },
        //         "id": "baf2a3419ee446c587cba75766d899f9"
        //     },
        //     {
        //         "attributes": {
        //             "AWS_INSTANCE_IPV4": "10.200.65.151",
        //             "AWS_INSTANCE_PORT": "8080",
        //             "AvailabilityZone": "us-west-2b",
        //             "DeploymentId": "arn:aws:ecs:us-west-2:123456789012:task-set/ecs-cluster-name/service-http/ecs-svc/2511542921236936597",
        //             "IdleTimeoutInSeconds": "0",
        //             "PerRequestTimeoutInSeconds": "0"
        //         },
        //         "id": "e3dab68c211d4dc187549dfa0a8f5369"
        //     }
        // ]
        // metrics-sd-service-http (cloud map instances)
        // [
        // {
        //     "attributes": {
        //         "AVAILABILITY_ZONE": "us-west-2a",
        //         "AWS_INIT_HEALTH_STATUS": "UNHEALTHY",
        //         "AWS_INSTANCE_IPV4": "10.200.65.58",
        //         "ECS_CLUSTER_NAME": "ecs-cluster-name",
        //         "ECS_SERVICE_NAME": "service-http",
        //         "ECS_TASK_DEFINITION_FAMILY": "ecs-service-http",
        //         "REGION": "us-west-2"
        //     },
        //     "id": "baf2a3419ee446c587cba75766d899f9"
        // },
        // {
        //     "attributes": {
        //         "AVAILABILITY_ZONE": "us-west-2b",
        //         "AWS_INIT_HEALTH_STATUS": "UNHEALTHY",
        //         "AWS_INSTANCE_IPV4": "10.200.65.151",
        //         "ECS_CLUSTER_NAME": "ecs-cluster-name",
        //         "ECS_SERVICE_NAME": "service-http",
        //         "ECS_TASK_DEFINITION_FAMILY": "ecs-service-http",
        //         "REGION": "us-west-2"
        //     },
        //     "id": "e3dab68c211d4dc187549dfa0a8f5369"
        // }
        // ]
    }

    /// <see cref="ICloudMapServiceDiscovery.GetInstance"/>
    /// <remarks>
    /// For more information about the GetInstance API operation, see <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TGetInstanceRequest.html">here</a>
    /// </remarks>
    public async Task<Instance> GetInstance(string serviceId, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        var request = new GetInstanceRequest
        {
            ServiceId = serviceId,
            InstanceId = instanceId
        };

        var response =
            await serviceDiscoveryClient.ExecuteWithRetryAsync(async () =>
                await serviceDiscoveryClient.GetInstanceAsync(request));

        return response.Instance;

        // TODO: Remove
        // metrics-sd-service-http (service discovery)
        // {
        //     "attributes": {
        //         "AWS_INSTANCE_IPV4": "10.200.65.58",
        //         "AWS_INIT_HEALTH_STATUS": "UNHEALTHY",
        //         "AVAILABILITY_ZONE": "us-west-2a",
        //         "ECS_CLUSTER_NAME": "ecs-cluster-name",
        //         "ECS_SERVICE_NAME": "service-http",
        //         "ECS_TASK_DEFINITION_FAMILY": "ecs-service-http",
        //         "REGION": "us-west-2"
        //     },
        //     "creatorRequestId": null,
        //     "id": "baf2a3419ee446c587cba75766d899f9"
        // }
        // service-http (service connect)
        // {
        //     "attributes": {
        //         "AWS_INSTANCE_IPV4": "10.200.65.58",
        //         "AWS_INSTANCE_PORT": "8080",
        //         "AvailabilityZone": "us-west-2a",
        //         "DeploymentId": "arn:aws:ecs:us-west-2:123456789012:task-set/ecs-cluster-name/service-http/ecs-svc/2511542921236936597",
        //         "IdleTimeoutInSeconds": "0",
        //         "PerRequestTimeoutInSeconds": "0"
        //     },
        //     "creatorRequestId": null,
        //     "id": "baf2a3419ee446c587cba75766d899f9"
        // }
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

        // TODO: Remove
        // Namespace Tags:
        // {
        //     "resourceArn": "arn:aws:servicediscovery:us-west-2:123456789012:namespace/ns-ke2ewbfvynxeu2ub",
        //     "tags": [
        //     {
        //         "key": "app_environment",
        //         "value": "dev"
        //     },
        //     {
        //         "key": "terraform",
        //         "value": "true"
        //     },
        //     {
        //         "key": "app_component",
        //         "value": "application"
        //     },
        //     {
        //         "key": "repository",
        //         "value": "terraform"
        //     }
        //     ]
        // }
        // Service tags (Service Connect):
        // {
        //     "resourceArn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-5kgcrz424wxiejsv",
        //     "tags": [
        //     {
        //         "key": "AmazonECSManaged",
        //         "value": "true"
        //     }
        //     ]
        // }
        // Service tags (Service Discovery):
        // {
        //     "resourceArn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-zo6epblsayj3h5za",
        //     "tags": [
        //     {
        //         "key": "METRICS_PATH",
        //         "value": "/metrics"
        //     },
        //     {
        //         "key": "METRICS_PORT",
        //         "value": "8080"
        //     },
        //     {
        //         "key": "ECS_METRICS_PATH",
        //         "value": "/metrics"
        //     },
        //     {
        //         "key": "ECS_METRICS_PORT",
        //         "value": "9779"
        //     },
        //     {
        //         "key": "app_environment",
        //         "value": "dev"
        //     },
        //     {
        //         "key": "app_component",
        //         "value": "application"
        //     },
        //     {
        //         "key": "terraform",
        //         "value": "true"
        //     },
        //     {
        //         "key": "repository",
        //         "value": "terraform"
        //     }
        //     ]
        // }
    }
}