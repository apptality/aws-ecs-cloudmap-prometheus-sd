using System.ComponentModel.DataAnnotations;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Class for configuring service discovery options
/// </summary>
public class DiscoveryOptions : IValidatableObject
{
    /// <summary>
    /// Semicolon separated string containing ECS clusters names to query for services.
    /// </summary>
    [Required]
    public string EcsClusters { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing regexes to match services names when ECS cluster is observed.
    /// Only those services that match any of the regular expressions supplied will be included.
    /// Default is ".*" (match all services).
    /// </summary>
    public string EcsServiceMatcherRegexes { get; init; } = ".*;";

    /// <summary>
    /// Semicolon separated string containing Cloud Map namespaces names to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of 'CloudMapNamespaces' or 'EcsClusters' must be configured.
    /// </remarks>
    public string CloudMapNamespaces { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing regexes to match services registered using Service Connect.
    /// Only those services that match any of the regular expressions supplied will be included.
    /// Default is ".*" (match all services).
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html">service-connect</a> docs.
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html">service-discovery</a> docs.
    /// </remarks>
    public string CloudMapServiceMatcherRegexes { get; init; } = ".*;";

    // Tags are resolved with the following priority:
    // * Cloud Map Service
    // * Cloud Map Namespace
    // * ECS Task
    // * ECS Service

    /// <summary>
    /// When non-empty, tags that match regex from ECS services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string EcsServiceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match regex from ECS tasks
    /// are included in the service discovery results as labels.
    /// </summary>
    /// <remarks>
    /// Note: tags are resolved from running tasks, not tasks definitions.
    /// If you would like to include tags from task definitions,
    /// consider modifying ECS service to propagate tags from task definitions to running tasks.
    /// You can read more <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ecs-using-tags.html#tag-resources">here</a>
    /// </remarks>
    public string EcsTaskTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match regex from Cloud Map namespaces
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapNamespaceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match regex from Cloud Map services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapServiceTagsRegex { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of static labels to include in the
    /// service discovery response as metadata.
    /// </summary>
    /// <remarks>
    /// Once parsed, provided labels and values will be added to all targets metadata
    /// </remarks>
    public string ExtraPrometheusLabels { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how long to cache the service discovery results in seconds. Default is five seconds.
    ///  </summary>
    /// <remarks>
    /// When caching is disabled, the service discovery results are queried every time a service response is requested.
    /// Min value - 0 seconds (no caching). Max value - 65535 seconds.
    /// <para>
    /// <b>Important:</b> It is strongly recommended to keep caching at the very least for 5 seconds to avoid rate limiting.
    /// </para>
    /// </remarks>
    [Range(0, 65535)]
    public ushort CacheTtlSeconds { get; set; } = 5;

    /// <summary>
    /// Validates the discovery options
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Keep track of validation results
        var validationResults = new List<ValidationResult>();

        // Ensure at least one Cloud Map namespace or ECS cluster is provided
        var ecsClusters = EcsClusters.Split(';').Where(ecs => !string.IsNullOrWhiteSpace(ecs)).ToList();

        if (ecsClusters.Count == 0)
        {
            validationResults.Add(new ValidationResult(
                "At least one ECS Cluster name must be specified.",
                new[] {nameof(EcsClusters)}));
        }

        // Get non-empty regexes for ECS service discovery
        var ecsServiceDiscoveryResults = EcsServiceMatcherRegexes.Split(';')
            .Where(regex => !string.IsNullOrWhiteSpace(regex)).ToList();

        var ecsServiceDiscoveryValidationResults = from regex in ecsServiceDiscoveryResults
            where !regex.IsValidRegex()
            select new ValidationResult(
                $"{nameof(EcsServiceMatcherRegexes)} contains an invalid regex: {regex}",
                new[] {nameof(EcsServiceMatcherRegexes)});

        // Get non-empty regexes for CloudMap service discovery
        var cloudMapServiceDiscoveryResults = CloudMapServiceMatcherRegexes.Split(';')
            .Where(regex => !string.IsNullOrWhiteSpace(regex)).ToList();

        var cloudMapServiceDiscoveryValidationResults = from regex in cloudMapServiceDiscoveryResults
            where !regex.IsValidRegex()
            select new ValidationResult(
                $"{nameof(CloudMapServiceMatcherRegexes)} contains an invalid regex: {regex}",
                new[] {nameof(CloudMapServiceMatcherRegexes)});

        // Aggregate validation results
        validationResults.AddRange(cloudMapServiceDiscoveryValidationResults);
        validationResults.AddRange(ecsServiceDiscoveryValidationResults);

        return validationResults;
    }
}