using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
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
    private readonly ICloudMapDiscoveryService _cloudMapDiscoveryService;
    private readonly IEcsDiscoveryService _ecsDiscoveryService;
    private readonly IEcsDiscovery _ecsDiscovery;
    private readonly DiscoveryOptions _discoveryOptions;

    // Default Ctor
    public DiscoveryService(
        ILogger<DiscoveryService> logger,
        IOptions<DiscoveryOptions> discoveryOptions,
        IEcsDiscovery ecsDiscovery,
        IEcsDiscoveryService ecsDiscoveryService,
        ICloudMapDiscoveryService cloudMapDiscoveryService
    )
    {
        _logger = logger;
        _discoveryOptions = discoveryOptions.Value;
        _ecsDiscovery = ecsDiscovery;
        _cloudMapDiscoveryService = cloudMapDiscoveryService;
        _ecsDiscoveryService = ecsDiscoveryService;
    }

    /// <inheritdoc cref="IDiscoveryService.Discover"/>
    public async Task<DiscoveryResult> Discover()
    {
        _logger.LogDebug("Starting infrastructure discovery");

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
    /// <remarks>
    /// Normally, this would have to be extracted into its own strategy class,
    /// but for the sake of simplicity, it's kept here.
    /// </remarks>
    internal Func<Task<DiscoveryResult>> GetDiscoveryExecutionStrategy()
    {
        // Pre-check if both ECS clusters and CloudMap namespaces are provided
        var hasCloudMapNamespacesProvided = _discoveryOptions.HasCloudMapNamespaces();
        var hasEcsClustersProvided = _discoveryOptions.HasEcsClusters();

        _logger.LogDebug("ECS clusters provided: {HasEcsClustersProvided}", hasEcsClustersProvided);
        _logger.LogDebug("CloudMap namespaces provided: {HasCloudMapNamespacesProvided}", hasCloudMapNamespacesProvided);
        _logger.LogDebug("Determining the best discovery strategy");

        // When both ECS cluster(s) and CloudMap namespace(s) provided via settings,
        // the result is the intersection of both:
        if (hasEcsClustersProvided && hasCloudMapNamespacesProvided)
        {
            return async () => new DiscoveryResult
            {
                DiscoveryOptions = _discoveryOptions,
                EcsClusters = await _ecsDiscoveryService.DiscoverEcsResources(),
                CloudMapNamespaces = await _cloudMapDiscoveryService.DiscoverCloudMapResources()
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
                var ecsClusters = await _ecsDiscoveryService.DiscoverEcsResources();

                var ecsServicesCloudMapServiceConnectNamespaces = ecsClusters
                    .SelectMany(c => c.Services)
                    .Select(s => s.Service.Deployments.FirstOrDefault(d => d.Status == "PRIMARY"))
                    .Where(s => s is {ServiceConnectConfiguration.Enabled: true})
                    .Select(s => s?.ServiceConnectConfiguration.Namespace.Split('/')[^1]!)
                    .Distinct()
                    .ToList();

                var cloudMapNamespaces = await _cloudMapDiscoveryService.DiscoverCloudMapResources(
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
        // and infer ECS clusters from CloudMap services. In this case, we have more flexibility
        // as we can infer ECS clusters from CloudMap services, both "Service Connect" and "Service Discovery".
        if (hasCloudMapNamespacesProvided)
        {
            return async () =>
            {
                var cloudMapNamespaces = await _cloudMapDiscoveryService.DiscoverCloudMapResources();

                // Load ECS services associated with CloudMap Service Connect namespaces provided
                var ecsCloudMapAssociatedServices = await Task.WhenAll(cloudMapNamespaces
                    .Select(ns => _ecsDiscovery.GetCloudMapServices(ns.Arn))
                    .ToList());

                // Convert associated ECS services to ECS clusters
                var cloudMapServiceConnectAssociatedClusters = ecsCloudMapAssociatedServices
                    .SelectMany(s => s.ServiceArns)
                    .Select(s =>
                    {
                        // Convert ecs service arns to ecs cluster arns
                        var chunks = s.Split('/');
                        var clusterBaseArn = chunks[0].Replace(":service", ":cluster");
                        // Extract ECS cluster ARN
                        return $"{clusterBaseArn}/{chunks[1]}";
                    })
                    .Distinct()
                    .ToList();

                // Discover ECS clusters registered using Service Discovery
                // inferred from CloudMap services instances
                var cloudMapServiceDiscoveryEcsClusters = cloudMapNamespaces
                    .SelectMany(ns => ns.ServiceSummaries)
                    .Where(s => s.ServiceType == CloudMapServiceType.ServiceDiscovery)
                    .SelectMany(s => s.InstanceSummaries)
                    .Select(i => i.EcsClusterName())
                    .Where(ecsClusterName => !string.IsNullOrEmpty(ecsClusterName))
                    .Distinct()
                    .ToList();

                // Combine inferred ECS clusters from CloudMap services
                var extraEcsClusters = new List<string>();
                if (cloudMapServiceDiscoveryEcsClusters.Count > 0)
                {
                    extraEcsClusters.AddRange(cloudMapServiceDiscoveryEcsClusters);
                }

                if (cloudMapServiceConnectAssociatedClusters.Count > 0)
                {
                    extraEcsClusters.AddRange(cloudMapServiceConnectAssociatedClusters);
                }

                // Discover ECS clusters with inferred ECS clusters
                var ecsClusters = await _ecsDiscoveryService.DiscoverEcsResources(extraEcsClusters: extraEcsClusters);

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
}