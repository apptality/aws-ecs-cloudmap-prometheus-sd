using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Contexts;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

public class DiscoveryService
{
    private DiscoveryOptions _discoveryOptions;
    private DiscoveryContext _discoveryContext;

    public DiscoveryService(DiscoveryOptions discoveryOptions, DiscoveryContext discoveryContext)
    {
        _discoveryOptions = discoveryOptions;
        _discoveryContext = discoveryContext;
    }

    public async Task Discover()
    {
        // Discover services
    }
}