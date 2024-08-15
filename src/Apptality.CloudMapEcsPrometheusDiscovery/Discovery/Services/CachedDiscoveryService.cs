using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;

/// <summary>
/// Cached discovery service that wraps an original discovery service
/// and caches its result for a period of time
/// </summary>
public class CachedDiscoveryService : IDiscoveryService
{
    private readonly ILogger<CachedDiscoveryService> _logger;
    private readonly IOptions<DiscoveryOptions> _discoveryOptions;
    private readonly IDiscoveryService _originalService;
    private readonly IMemoryCache _cache;

    public CachedDiscoveryService(
        ILogger<CachedDiscoveryService> logger,
        IOptions<DiscoveryOptions> discoveryOptions,
        IDiscoveryService originalService,
        IMemoryCache cache)
    {
        _logger = logger;
        _discoveryOptions = discoveryOptions;
        _originalService = originalService;
        _cache = cache;
    }

    /// <summary>
    /// Returns the discovery result from the original service, caching it for a period of time
    /// </summary>
    public async Task<DiscoveryResult> Discover()
    {
        var cacheKey = "discovery-service-cache-key";
        if (_cache.TryGetValue<DiscoveryResult>(cacheKey, out var discoveryResult))
        {
            _logger.LogDebug("Returning cached discovery result");
            if (discoveryResult != null)
            {
                return discoveryResult;
            }
        }

        _logger.LogDebug("Cache miss, calling original service");
        discoveryResult = await _originalService.Discover();
        _cache.Set(cacheKey, discoveryResult, TimeSpan.FromSeconds(_discoveryOptions.Value.CacheTtlSeconds));

        return discoveryResult;
    }
}