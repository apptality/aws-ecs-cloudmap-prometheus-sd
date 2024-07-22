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