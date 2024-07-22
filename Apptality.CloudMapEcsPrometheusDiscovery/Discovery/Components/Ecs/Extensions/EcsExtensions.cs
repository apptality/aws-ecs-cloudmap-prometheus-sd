namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Extensions;

/// <summary>
/// Class containing extension methods for ECS services
/// </summary>
public static class EcsExtensions
{
    /// <summary>
    /// Get ECS cluster name from ECS service ARN
    /// </summary>
    /// <param name="ecsServiceArn">
    /// Ecs service ARN.
    /// Example: "arn:aws:ecs:us-west-2:12345678901:service/ecs-cluster/service-http"
    /// </param>
    /// <returns>
    /// Name of ECS cluster.
    /// Example: "ecs-cluster"
    /// </returns>
    public static string GetEcsClusterName(this string ecsServiceArn)
    {
        var parts = ecsServiceArn.Split('/');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}