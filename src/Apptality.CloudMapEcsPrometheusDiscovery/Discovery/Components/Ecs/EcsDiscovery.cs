using Amazon.ECS;
using Amazon.ECS.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Extensions;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;

/// <summary>
/// Concrete implementation of the ECS service discovery interface
/// </summary>
public class EcsDiscovery(
    ILogger<EcsDiscovery> logger,
    IAmazonECS ecsClient
) : IEcsDiscovery
{
    /// <inheritdoc cref="IEcsDiscovery.GetClusters"/>
    /// <remarks>
    /// Read more about the DescribeClusters API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeClustersRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<EcsCluster>> GetClusters(ICollection<string> clusterNames)
    {
        if (clusterNames.Count == 0) return [];

        logger.LogDebug("Fetching ECS clusters: {@EcsClusters}", clusterNames);

        var request = new DescribeClustersRequest
        {
            Clusters = clusterNames.ToList(),
            Include = ["TAGS"]
        };

        var response = await ecsClient.DescribeClustersAsync(request);

        // Report any failures
        if (response.Failures.Count > 0)
        {
            logger.LogWarning("Failures while fetching ECS clusters: {@Failures}", response.Failures);
        }

        return response.Clusters.Select(c => new EcsCluster
        {
            Cluster = c
        }).ToList();
    }

    /// <inheritdoc cref="IEcsDiscovery.GetClusterServices(string)"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListServicesRequest.html">here</a>
    /// </remarks>
    public async Task<EcsClusterServices> GetClusterServices(string clusterArn)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));

        var request = new ListServicesRequest {Cluster = clusterArn};
        logger.LogDebug("Fetching ECS services for cluster: {ClusterArn}", clusterArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListServicesAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var serviceArns = responses.SelectMany(r => r.ServiceArns).ToArray();
        logger.LogDebug("In ECS cluster {ClusterArn}, found {ServiceCount} services: {@ServiceArns}", clusterArn,
            serviceArns.Length, serviceArns);

        return new EcsClusterServices(clusterArn, serviceArns);
    }

    /// <inheritdoc cref="IEcsDiscovery.GetCloudMapServices(string)"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListServicesByNamespaceRequest.html">here</a>
    /// </remarks>
    public async Task<EcsCloudMapServices> GetCloudMapServices(string namespaceArn)
    {
        if (string.IsNullOrWhiteSpace(namespaceArn))
            throw new ArgumentException("CloudMap ARN must be provided.", nameof(namespaceArn));

        var request = new ListServicesByNamespaceRequest {Namespace = namespaceArn};
        logger.LogDebug("Fetching ECS services for CloudMap Namespace: {Namespace}", namespaceArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListServicesByNamespaceAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var serviceArns = responses.SelectMany(r => r.ServiceArns).ToArray();
        logger.LogDebug("In CloudMap namespace {Namespace} found following services: {@ServiceArns}", namespaceArn,
            serviceArns);

        return new EcsCloudMapServices(namespaceArn, serviceArns);
    }

    /// <inheritdoc cref="IEcsDiscovery.DescribeServices"/>
    /// <remarks>
    /// Read more about the DescribeServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeServicesRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<EcsService>> DescribeServices(ICollection<string> serviceArns)
    {
        // First, group service ARNs by ECS cluster name
        var serviceArnsByCluster = serviceArns
            .GroupBy(arn => arn.GetEcsClusterName())
            .ToDictionary(g => g.Key, g => g.ToArray());

        var services = new List<EcsService>();

        foreach (var (clusterName, clusterServiceArns) in serviceArnsByCluster)
        {
            // Divide clusterServiceArns into batches of 10
            var clusterServiceArnsBatches = clusterServiceArns
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / 10)
                .Select(g => g.Select(x => x.Value).ToList())
                .ToList();

            foreach (var ecsServiceArnsBatch in clusterServiceArnsBatches)
            {
                // This request supports describing up to 10 services at a time
                var request = new DescribeServicesRequest
                {
                    Cluster = clusterName,
                    Include = ["TAGS"],
                    Services = ecsServiceArnsBatch
                };

                logger.LogDebug(
                    "Fetching ECS services batch for cluster {ClusterName} with the following ECS Service ARNs: {@ServiceArns}",
                    clusterName, ecsServiceArnsBatch);

                var response = await ecsClient.DescribeServicesAsync(request);

                // Trim events, as these are non-relevant and just add noise
                response.Services.ForEach(s => s.Events = null);

                logger.LogDebug(
                    "Fetched ECS services batch for cluster {ClusterName} with the following response: {@Services}",
                    clusterName, response.Services);

                services.AddRange(
                    response.Services.Select(s => new EcsService
                    {
                        ClusterArn = s.ClusterArn,
                        Service = s
                    })
                );
            }
        }

        return services;
    }

    /// <inheritdoc cref="IEcsDiscovery.GetRunningTasks"/>
    /// <remarks>
    /// Read more about the ListTasks API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListTasksRequest.html">here</a>
    /// </remarks>
    public async Task<EcsServiceTasks> GetRunningTasks(string clusterArn, string serviceArn)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));
        if (string.IsNullOrWhiteSpace(serviceArn))
            throw new ArgumentException("Service ARN must be provided.", nameof(serviceArn));

        var request = new ListTasksRequest
        {
            Cluster = clusterArn,
            ServiceName = serviceArn,
            DesiredStatus = DesiredStatus.RUNNING
        };

        logger.LogDebug("Fetching running tasks for ECS service {ServiceArn} in cluster {ClusterArn}", serviceArn,
            clusterArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListTasksAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var runningTaskArns = responses.SelectMany(response => response.TaskArns).ToArray();

        logger.LogDebug(
            "In ECS cluster {ClusterArn}, found {RunningTaskCount} running tasks for service {ServiceArn}: {@RunningTaskArns}",
            clusterArn, runningTaskArns.Length, serviceArn, runningTaskArns);

        return new EcsServiceTasks(clusterArn, serviceArn, runningTaskArns);
    }

    /// <inheritdoc cref="IEcsDiscovery.DescribeTasks"/>
    /// <remarks>
    /// Read more about the DescribeTasks API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeTasksRequest.html">here</a>
    /// </remarks>
    public async Task<ICollection<EcsTask>> DescribeTasks(string clusterArn, string serviceArn, string[] taskArns)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));

        if (taskArns.Length == 0) return [];

        // Divide taskArns into batches of 100, as this is the maximum number of tasks that can be described at once
        var taskArnsBatches = taskArns
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 100)
            .Select(g => g.Select(x => x.Value).ToList())
            .ToList();

        var tasks = new List<EcsTask>();

        // Fetch tasks in batches
        foreach (var taskArnsBatch in taskArnsBatches)
        {
            var request = new DescribeTasksRequest {Cluster = clusterArn, Tasks = taskArnsBatch, Include = ["TAGS"]};

            var response = await ecsClient.DescribeTasksAsync(request);
            var ecsTasks = response.Tasks.Select(t => new EcsTask
            {
                ClusterArn = t.ClusterArn,
                ServiceArn = serviceArn,
                Task = t
            }).ToList();

            tasks.AddRange(ecsTasks);
        }

        logger.LogDebug(
            "Fetched ECS tasks for cluster {ClusterArn} and service {ServiceArn}: {@Tasks}",
            clusterArn, serviceArn, tasks);

        return tasks;
    }
}