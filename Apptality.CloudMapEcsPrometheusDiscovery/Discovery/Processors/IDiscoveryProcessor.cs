using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Processors;

/// <summary>
/// Represent interface for discovery processors.
/// Processor is an atomic unit of business logic, desinged
/// to modify shared state.
/// </summary>
public interface IDiscoveryProcessor
{
    /// <summary>
    /// Takes in a shared state and applies
    /// </summary>
    /// <param name="result">Object representing shared state changes are being made upon</param>
   public Task Process(DiscoveryResult result);
}