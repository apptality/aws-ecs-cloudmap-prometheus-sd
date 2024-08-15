namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components;

/// <summary>
/// Simple wrapper for holding AWS resource information
/// </summary>
/// <param name="Arn">ARN of the resource</param>
public record AwsResource(string Arn);