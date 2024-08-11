using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Record representing a Cloud Map resource identifier and its tags
/// </summary>
/// <param name="ResourceArn">ARN of the resource</param>
/// <param name="Tags">Resource Tags</param>
public record CloudMapResourceTags(string ResourceArn, ResourceTag[] Tags);