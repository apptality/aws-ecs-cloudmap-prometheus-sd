using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

/// <summary>
/// Record representing a ServiceDiscovery resource identifier and its tags
/// </summary>
/// <param name="ResourceArn"></param>
/// <param name="Tags"></param>
public record ServiceDiscoveryResourceTags(string ResourceArn, Tag[] Tags);