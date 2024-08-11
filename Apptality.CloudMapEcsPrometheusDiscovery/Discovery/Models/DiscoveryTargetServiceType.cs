using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Filters;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

/// <summary>
/// Helps to identify the type of service that is being discovered
/// </summary>
/// <remarks>
/// Please read more about the differences <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/interconnecting-services.html">here</a>
/// </remarks>
public enum DiscoveryTargetServiceType
{
    /// <summary>
    /// Represents the fact that the service is a service connect service
    /// </summary>
    ServiceConnect = 0,

    /// <summary>
    /// Represents the fact that the service is a service discovery service
    /// </summary>
    ServiceDiscovery = 10,
}

/// <summary>
/// Helper class to identify the type of service that is being discovered.
/// </summary>
public static class DiscoveryTargetServiceTypeIdentifier
{
    /// <summary>
    /// We can identify a service connect service by the presence of the AmazonECSManaged tag
    /// </summary>
    private static readonly ResourceTagSelectorFilter ServiceConnectFilter = new ResourceTagSelectorFilter(
        [
            new ResourceTagSelector
            {
                Key = "AmazonECSManaged",
                Value = "true",
            }
        ]
    );

    public static DiscoveryTargetServiceType IdentifyServiceType(
        CloudMapService cloudMapService
    )
    {
        // The best indicator of a service connect service is
        // Description of the service containing value similar to the following:
        // "Managed by arn:aws:ecs:us-west-1:123456789012:service/ecs-cluster/service-name"
        var serviceSummaryDescription = cloudMapService.ServiceSummary.Description.ToLower();
        if (serviceSummaryDescription.Contains("arn:") &&
            serviceSummaryDescription.Contains(":ecs:") &&
            serviceSummaryDescription.Contains(":service")
           )
        {
            return DiscoveryTargetServiceType.ServiceConnect;
        }

        // If we can match by tag - highly likely service connect
        if (ServiceConnectFilter.Matches(cloudMapService.Tags))
        {
            return DiscoveryTargetServiceType.ServiceConnect;
        }

        // If we were unable to identify the service as a service connect service
        // we can assume it is a service discovery service
        return DiscoveryTargetServiceType.ServiceDiscovery;
    }
}