using Amazon.ServiceDiscovery.Model;
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
    Task<ICollection<NamespaceSummary>> GetNamespaces(ICollection<string> namespaceNames);

    /// <summary>
    /// Lists all services in a given namespace
    /// </summary>
    /// <param name="namespaceIds">Identifiers of namespaces. Example: ['ns-ke2ewbfvynxeu2ub', ... ]</param>
    /// <returns>
    /// List of services, or null if no namespaces are found
    /// </returns>
    Task<ICollection<ServiceDiscoveryServiceSummary>> GetServices(string namespaceIds);

    /// <summary>
    /// Lists all instances of a given service
    /// </summary>
    /// <param name="serviceId">Identifier of the service to list instances for</param>
    /// <returns>Response object from the ListInstances operation, or null if service is not found</returns>
    Task<ICollection<ServiceDiscoveryInstanceSummary>> GetServiceInstances(string serviceId);

    /// <summary>
    /// Get information about a service instance
    /// </summary>
    /// <param name="serviceId">CloudMap service Id</param>
    /// <param name="instanceId">Identifier of a service instance</param>
    /// <returns>Response object containing information about service instance</returns>
    Task<Instance> GetInstance(string serviceId, string instanceId);

    /// <summary>
    /// Retrieves tags for a given service discovery resource
    /// </summary>
    /// <param name="resourceArn">ARN of the resource to fetch tags for</param>
    /// <returns>Object containing the resource ARN and its tags</returns>
    Task<ServiceDiscoveryResourceTags> GetTags(string resourceArn);
}