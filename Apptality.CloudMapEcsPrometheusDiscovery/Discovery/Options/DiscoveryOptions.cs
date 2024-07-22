using System.ComponentModel.DataAnnotations;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Class for configuring service discovery options
/// </summary>
public class DiscoveryOptions : IValidatableObject
{
    /// <summary>
    /// Semicolon separated string containing Cloud Map namespaces names to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of 'CloudMapNamespaces' or 'EcsClusters' must be configured.
    /// </remarks>
    public string CloudMapNamespaces { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing ECS clusters names to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of 'CloudMapNamespaces' or 'EcsClusters' must be configured.
    /// </remarks>
    public string EcsClusters { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing regexes to match services registered using Service Connect.
    /// Default is ".*" (match all services). Provide empty string to disable "Service Connect" discovery.
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html">service-connect</a> docs.
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html">service-discovery</a> docs.
    /// </remarks>
    public string CloudMapServiceMatcherRegexes { get; init; } = ".*;";

    // Tags are resolved with the following priority:
    // * EC2 Instance
    // * ECS Service
    // * ECS Task
    // * Cloud Map Namespace
    // * Cloud Map Service

    /// <summary>
    /// When non-empty, tags from ECS services are included in the service discovery results as labels.
    /// </summary>
    public string EcsServiceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags from ECS tasks are included in the service discovery results as labels.
    /// </summary>
    public string EcsTaskTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, and ECS cluster is composed of EC2 instances,
    /// tags from the EC2 instances are included in the service discovery results as labels.
    /// </summary>
    public string Ec2InstanceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags from Cloud Map namespaces are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapNamespaceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags from Cloud Map services are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapServiceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how long to cache the service discovery results in seconds. Default is one second.
    ///  </summary>
    /// <remarks>
    /// When caching is disabled, the service discovery results are queried every time a service response is requested.
    /// Min value - 0 seconds (no caching). Max value - 65535 seconds.
    /// <para>
    /// <b>Important:</b> It is strongly recommended to keep caching at the very least for 1 second to avoid rate limiting.
    /// </para>
    /// </remarks>
    [Range(0, 65535)]
    public ushort CacheTtlSeconds { get; set; } = 1;

    /// <summary>
    /// Validates the discovery options
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Keep track of validation results
        var validationResults = new List<ValidationResult>();

        // Ensure at least one Cloud Map namespace or ECS cluster is provided
        var cloudMapNamespaces = CloudMapNamespaces.Split(';').Where(ns => !string.IsNullOrWhiteSpace(ns)).ToList();
        var ecsClusters = EcsClusters.Split(';').Where(ecs => !string.IsNullOrWhiteSpace(ecs)).ToList();

        if (cloudMapNamespaces.Count == 0 && ecsClusters.Count == 0)
        {
            validationResults.Add(new ValidationResult(
                $"{nameof(CloudMapNamespaces)} and {nameof(EcsClusters)} are both empty. At least one ServiceDiscovery namespace or ECS Cluster name must be specified.",
                new[] {nameof(CloudMapNamespaces), nameof(EcsClusters)}));
        }

        // Get non-empty regexes for Service Discovery
        var serviceDiscoveryResults = CloudMapServiceMatcherRegexes.Split(';')
            .Where(regex => !string.IsNullOrWhiteSpace(regex)).ToList();

        var serviceDiscoveryValidationResults = from regex in serviceDiscoveryResults
            where !regex.IsValidRegex()
            select new ValidationResult(
                $"{nameof(CloudMapServiceMatcherRegexes)} contains an invalid regex: {regex}",
                new[] {nameof(CloudMapServiceMatcherRegexes)});

        // Aggregate validation results
        validationResults.AddRange(serviceDiscoveryValidationResults);

        return validationResults;
    }
}