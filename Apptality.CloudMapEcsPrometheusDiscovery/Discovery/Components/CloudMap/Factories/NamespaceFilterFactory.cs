using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap.Factories;

internal static class NamespaceFilterFactory
{
    /// <summary>
    /// Generates a list of namespace filters from a string of semicolon separated ServiceDiscovery namespace names
    /// </summary>
    /// <param name="cloudMapNamespaceNames">Semicolon separated list of ServiceDiscovery namespace names</param>
    /// <returns>List of namespace filters</returns>
    /// <remarks>
    /// Read more <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ServiceDiscovery/TNamespaceFilter.html">here</a>
    /// </remarks>
    internal static List<NamespaceFilter> Create(string cloudMapNamespaceNames)
    {
        if (string.IsNullOrWhiteSpace(cloudMapNamespaceNames)) return [];

        var namespaceNames = cloudMapNamespaceNames.Split(';').Where(ns => !string.IsNullOrWhiteSpace(ns));

        return namespaceNames.Select(namespaceName => new NamespaceFilter
            {Condition = FilterCondition.EQ, Name = NamespaceFilterName.NAME, Values = [namespaceName]}).ToList();
    }
}