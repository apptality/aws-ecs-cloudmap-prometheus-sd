using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Factories;

internal static class ServiceFilterFactory
{
    /// <summary>
    /// Constructs a list of service filters based on the provided namespaceIds
    /// </summary>
    /// <param name="namespaceIds">Collection of namespace identifiers. Namespace identifier example: ns-ke2ewbfvynxeu2ub</param>
    /// <returns>
    /// A complex type that lets you specify the namespaces that you want to list services for.
    ///</returns>
    /// <remarks>
    /// Read more <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TServiceFilter.html">here</a>
    /// </remarks>
    internal static List<ServiceFilter> Create(string[] namespaceIds)
    {
        if (namespaceIds.Length == 0) return [];

        return namespaceIds.Select(namespaceId => new ServiceFilter
        {
            Condition = FilterCondition.EQ,
            Name = ServiceFilterName.NAMESPACE_ID,
            Values = [namespaceId]
        }).ToList();
    }
}