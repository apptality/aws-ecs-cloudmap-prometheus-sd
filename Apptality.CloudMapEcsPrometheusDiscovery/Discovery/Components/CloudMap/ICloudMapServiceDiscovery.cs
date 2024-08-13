using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;

public interface ICloudMapServiceDiscovery
{
    /// <summary>
    /// Resolves the namespaces
    /// </summary>
    /// <returns>
    /// List of namespaces, or null if no namespaces are found
    /// </returns>
    Task<ICollection<CloudMapNamespace>> GetNamespaces(ICollection<string> namespaceNames);

    /// <summary>
    /// Lists all services in a given namespace
    /// </summary>
    /// <param name="namespaceIds">Identifiers of namespaces. Example: ['ns-ke2ewbfvynxeu2ub', ... ]</param>
    /// <returns>
    /// List of services, or null if no namespaces are found
    /// </returns>
    Task<ICollection<CloudMapService>> GetServices(string namespaceIds);

    /// <summary>
    /// Lists all instances of a given service
    /// </summary>
    /// <param name="serviceId">Identifier of the service to list instances for</param>
    /// <returns>Response object from the ListInstances operation, or null if service is not found</returns>
    Task<ICollection<CloudMapServiceInstance>> GetServiceInstances(string serviceId);
    
    /// <summary>
    /// Retrieves tags for a given service discovery resource
    /// </summary>
    /// <param name="resourceArn">ARN of the resource to fetch tags for</param>
    /// <returns>Object containing the resource ARN and its tags</returns>
    Task<CloudMapResourceTags> GetTags(string resourceArn);
}