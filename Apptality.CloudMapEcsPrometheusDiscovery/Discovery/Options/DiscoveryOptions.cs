using System.ComponentModel.DataAnnotations;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Class for configuring service discovery options
/// </summary>
public class DiscoveryOptions : IValidatableObject
{
    /// <summary>
    /// Specifies how long to cache the service discovery results in seconds. Default is 0 (no caching).
    ///  </summary>
    /// <remarks>
    /// When caching is disabled, the service discovery results are queried every time a service response is requested.
    /// Min value - 0 seconds (no caching). Max value - 65535 seconds.
    /// </remarks>
    [Range(0, 65535)]
    public ushort CacheTtlSeconds { get; set; } = 0;

    /// <summary>
    /// Semicolon separated list of Cloud Map namespaces to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of 'CloudMapNamespaces' or 'EcsClusters' must be configured.
    /// </remarks>
    public string CloudMapNamespaces { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated list of ECS clusters to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of 'CloudMapNamespaces' or 'EcsClusters' must be configured.
    /// </remarks>
    public string EcsClusters { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated list of regexes to match services to registered using Service Connect.
    /// Default is ".*" (match all services). Provide empty string to disable "Service Connect" discovery.
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html">service-connect</a> docs.
    /// </remarks>
    public string ServiceConnectMatcherRegexes { get; init; } = ".*;";

    /// <summary>
    /// Semicolon separated list of regexes to match services to registered using Service Discovery.
    /// Default is ".*" (match all services). Provide empty string to disable "Service Discovery" discovery.
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html">service-discovery</a> docs.
    /// </remarks>
    public string ServiceDiscoveryMatcherRegexes { get; set; } = ".*;";

    /// <summary>
    /// When enabled, tags from ECS services are included in the service discovery results as labels.
    /// </summary>
    /// <remarks>
    /// Tags priority: ECS Service > ECS Task > EC2 Instance > Cloud Map Namespace > Cloud Map Service > Cloud Map Service Instance
    /// </remarks>
    public bool IncludeEcsServiceTags { get; set; } = true;

    /// <summary>
    /// When enabled, tags from ECS tasks are included in the service discovery results as labels.
    /// </summary>
    public bool IncludeEcsTaskTags { get; set; } = true;

    /// <summary>
    /// When enabled, and ECS cluster is composed of EC2 instances, tags from the EC2 instances are included in the service discovery results as labels.
    /// </summary>
    public bool IncludeEc2InstanceTags { get; set; } = true;

    /// <summary>
    /// When enabled, tags from Cloud Map namespaces are included in the service discovery results as labels.
    /// </summary>
    public bool IncludeCloudMapNamespaceTags { get; set; } = true;

    /// <summary>
    /// When enabled, tags from Cloud Map services are included in the service discovery results as labels.
    /// </summary>
    public bool IncludeCloudMapServiceTags { get; set; } = true;

    /// <summary>
    /// When enabled, tags from Cloud Map service instances are included in the service discovery results as labels.
    /// </summary>
    public bool IncludeCloudMapServiceInstanceTags { get; set; } = true;


    /// <summary>
    /// Validates the discovery options
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Keep track of validation results
        var validationResults = new List<ValidationResult>();

        // Get non-empty regexes for Service Connect
        var serviceConnectResults = ServiceConnectMatcherRegexes.Split(';')
            .Where(regex => !string.IsNullOrWhiteSpace(regex)).ToList();

        var serviceConnectValidationResults = from regex in serviceConnectResults
            where !regex.IsValidRegex()
            select new ValidationResult(
                $"{nameof(ServiceConnectMatcherRegexes)} contains an invalid regex: {regex}",
                new[] {nameof(ServiceConnectMatcherRegexes)});

        // Get non-empty regexes for Service Discovery
        var serviceDiscoveryResults = ServiceDiscoveryMatcherRegexes.Split(';')
            .Where(regex => !string.IsNullOrWhiteSpace(regex)).ToList();

        var serviceDiscoveryValidationResults = from regex in serviceDiscoveryResults
            where !regex.IsValidRegex()
            select new ValidationResult(
                $"{nameof(ServiceDiscoveryMatcherRegexes)} contains an invalid regex: {regex}",
                new[] {nameof(ServiceDiscoveryMatcherRegexes)});

        // If neither Service Connect nor Service Discovery is enabled, add an error
        if (serviceConnectResults.Count == 0 && serviceDiscoveryResults.Count == 0)
            validationResults.Add(new ValidationResult(
                $"Both {nameof(ServiceConnectMatcherRegexes)} and {nameof(ServiceDiscoveryMatcherRegexes)} are empty. At least one must be configured.",
                new[] {nameof(ServiceConnectMatcherRegexes), nameof(ServiceDiscoveryMatcherRegexes)}));

        // Ensure at least one Cloud Map namespace or ECS cluster is provided
        var cloudMapNamespaces = CloudMapNamespaces.Split(';').Where(ns => !string.IsNullOrWhiteSpace(ns)).ToList();
        var ecsClusters = EcsClusters.Split(';').Where(ecs => !string.IsNullOrWhiteSpace(ecs)).ToList();

        if (cloudMapNamespaces.Count == 0 && ecsClusters.Count == 0)
        {
            validationResults.Add(new ValidationResult(
                $"{nameof(CloudMapNamespaces)} and {nameof(EcsClusters)} are both empty. At least one CloudMap namespace or ECS Cluster name must be specified.",
                new[] {nameof(CloudMapNamespaces), nameof(EcsClusters)}));
        }

        // Aggregate validation results
        validationResults.AddRange(serviceDiscoveryValidationResults);
        validationResults.AddRange(serviceConnectValidationResults);

        return validationResults;
    }
}