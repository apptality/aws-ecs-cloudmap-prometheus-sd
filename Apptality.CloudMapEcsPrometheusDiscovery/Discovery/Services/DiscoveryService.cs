using Amazon.ECS.Model;
using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

public class DiscoveryService
{
    private readonly ILogger<DiscoveryService> _logger;
    private readonly DiscoveryResult _discoveryResult;
    private readonly IEcsDiscovery _ecsDiscovery;
    private readonly ICloudMapServiceDiscovery _cloudMapServiceDiscovery;
    private readonly DiscoveryOptions _discoveryOptions;

    public DiscoveryService(
        ILogger<DiscoveryService> logger,
        IOptions<DiscoveryOptions> discoveryOptions,
        DiscoveryResult discoveryResult,
        IEcsDiscovery ecsDiscovery,
        ICloudMapServiceDiscovery cloudMapServiceDiscovery
    )
    {
        _logger = logger;
        _discoveryOptions = discoveryOptions.Value;
        _discoveryResult = discoveryResult;
        _ecsDiscovery = ecsDiscovery;
        _cloudMapServiceDiscovery = cloudMapServiceDiscovery;
    }

    public async Task<DiscoveryResult> Discover()
    {
        // Get ECS clusters
        var clusters = await DiscoverEcsClusters();
        var namespaces = await DiscoverCloudMapNamespaces();

        // Get CloudMap namespaces
        var ecsCloudMapServiceConnectNamespaces = clusters.Select(c => c.ServiceConnectDefaults.Namespace).ToList();
        var cloudMapServiceConnectNamespaces = namespaces.Select(n => n.Arn).ToList();
        // Join and deduplicate namespaces
        cloudMapServiceConnectNamespaces.AddRange(ecsCloudMapServiceConnectNamespaces);
        cloudMapServiceConnectNamespaces = cloudMapServiceConnectNamespaces.Distinct().ToList();

        // Start discovering CloudMap services for each namespace concurrently
        var ecsCloudMapServiceConnectServices = cloudMapServiceConnectNamespaces
            .Select(n => _ecsDiscovery.GetCloudMapServices(n)).ToList();
        var ecsCloudMapServiceConnectServicesResults = await Task.WhenAll(ecsCloudMapServiceConnectServices);
        // Now, infer ECS clusters ARNs from the discovered services
        var ecsClustersArnsInferredFromCloudMap = ecsCloudMapServiceConnectServicesResults
            .SelectMany(s => s.ServiceArns)
            .Select(s =>
            {
                var chunks = s.Split('/');
                var clusterBaseArn = chunks[0].Replace(":service", ":cluster");
                // Extract ECS cluster ARN
                return $"{clusterBaseArn}/{chunks[1]}";
            })
            .Distinct()
            .ToList();

        // Build and deduplicate a complete list of ECS clusters ARNs
        var allEcsClusterArns = clusters.Select(c => c.ClusterArn).ToList();
        allEcsClusterArns.AddRange(ecsClustersArnsInferredFromCloudMap);
        allEcsClusterArns = allEcsClusterArns.Distinct().ToList();

        // Start resolving ECS services for each cluster concurrently
        var ecsClusterServices = allEcsClusterArns.Select(arn => _ecsDiscovery.GetClusterServices(arn)).ToList();
        var ecsClusterServicesResults = await Task.WhenAll(ecsClusterServices);

        var cloudMapServicesIds = cloudMapServiceConnectNamespaces.Select(s => s.Split('/')[1]).ToList();
        var cloudMapServices = cloudMapServicesIds.Select(namespaceId => _cloudMapServiceDiscovery.GetServices(namespaceId)).ToList();
        var cloudMapServicesResults = (await Task.WhenAll(cloudMapServices)).SelectMany(s => s).ToList();

        // For each CloudMap namespace, get its tags
        var cloudMapNamespacesTags =
            cloudMapServiceConnectNamespaces.Select(ns => _cloudMapServiceDiscovery.GetTags(ns)).ToList();
        var cloudMapNamespacesTagsResults = await Task.WhenAll(cloudMapNamespacesTags);

        // For each CloudMap service, get its tags
        var cloudMapServicesTags =
            cloudMapServicesResults.Select(ss => _cloudMapServiceDiscovery.GetTags(ss.Arn)).ToList();
        var cloudMapServicesTagsResults = await Task.WhenAll(cloudMapServicesTags);

        // Get CloudMap instances for each service
        var cloudMapServiceInstances = cloudMapServicesResults.Select(s => _cloudMapServiceDiscovery.GetServiceInstances(s.Id)).ToList();
        var cloudMapServiceInstancesResults = await Task.WhenAll(cloudMapServiceInstances);
        // This finalizes the discovery of CloudMap services

        // Get ECS services for each cluster
        var ecsClusterServicesArns = ecsClusterServicesResults.SelectMany(c => c.ServiceArns).ToList();
        var ecsServices = await _ecsDiscovery.DescribeServices(ecsClusterServicesArns);

        // Now we need to describe running tasks
        // First, group all service ARNs by cluster ARN
        var ecsClusterServicesGroups = ecsServices.Select(s => new { s.ServiceArn, s.ClusterArn }).ToList();
        var ecsRunningTasks = ecsClusterServicesGroups
            .Select(s => _ecsDiscovery.GetRunningTasks(s.ClusterArn, s.ServiceArn)).ToList();
        var ecsRunningTasksResults = await Task.WhenAll(ecsRunningTasks);

        var ecsServiceRunningTasks = ecsRunningTasksResults.Select(s=> _ecsDiscovery.DescribeTasks(s.ClusterArn, s.RunningTaskArns)).ToList();
        var ecsServiceRunningTasksResults = await Task.WhenAll(ecsServiceRunningTasks);
        // Well, now this finalizes the discovery

        // Start building the discovery result
        // TODO: Build the discovery result

        return _discoveryResult;
    }

    internal async Task<List<NamespaceSummary>> DiscoverCloudMapNamespaces()
    {
        // Get CloudMap namespaces from configuration
        var cloudMapNamespacesNames = _discoveryOptions.GetCloudMapNamespaceNames();

        if (cloudMapNamespacesNames.Length <= 0) return [];

        var cloudMapNamespaces = await _cloudMapServiceDiscovery.GetNamespaces(cloudMapNamespacesNames);
        if (cloudMapNamespaces.Count == 0)
        {
            _logger.LogDebug("No CloudMap namespaces found for the provided names: {@CloudMapNamespaces}",
                _discoveryOptions.CloudMapNamespaces);
        }

        return cloudMapNamespaces;
    }

    internal async Task<List<Cluster>> DiscoverEcsClusters()
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
}