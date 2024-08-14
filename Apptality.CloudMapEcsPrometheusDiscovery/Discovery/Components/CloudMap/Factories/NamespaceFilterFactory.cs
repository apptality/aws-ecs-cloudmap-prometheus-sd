using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Factories;

internal static class NamespaceFilterFactory
{
    /// <summary>
    /// Generates a list of namespace filters
    /// </summary>
    /// <param name="cloudMapNamespaceNames">Array containing CloudMap namespace names</param>
    /// <returns>List of namespace filters</returns>
    /// <remarks>
    /// Read more <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TNamespaceFilter.html">here</a>
    /// </remarks>
    internal static List<NamespaceFilter> Create(ICollection<string> cloudMapNamespaceNames)
    {
        var namespaceNames = cloudMapNamespaceNames.Where(ns => !string.IsNullOrWhiteSpace(ns));

        // TODO: Update the code to allow filtering based on ARNs as well as names
        return namespaceNames.Select(namespaceName => new NamespaceFilter
        {
            Condition = FilterCondition.EQ,
            Name = NamespaceFilterName.NAME,
            Values = [namespaceName]
        }).ToList();
    }
}