using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Options validator for the DiscoveryOptions. Allows validating the options
/// without using reflection, thus playing nicely with assembly trimming.
/// </summary>
[OptionsValidator]
public partial class DiscoveryOptionsValidator : IValidateOptions<DiscoveryOptions>
{
    // read more: https://learn.microsoft.com/en-us/dotnet/core/extensions/options-validation-generator
}

/// <summary>
/// Custom validator for the DiscoveryOptions to validate the options
/// using custom logic.
/// </summary>
public class DiscoveryOptionsCustomValidator : IValidateOptions<DiscoveryOptions>
{
    public ValidateOptionsResult Validate(string? name, DiscoveryOptions options)
    {
        var validationErrors = new List<ValidationResult>();

        // Ensure at least one Cloud Map namespace or ECS cluster is provided
        var ecsClusters = options.GetEcsClustersNames();
        var cloudMapNamespaces = options.GetCloudMapNamespaceNames();

        // If both are empty, add a validation error
        if (ecsClusters.Length == 0 && cloudMapNamespaces.Length == 0)
        {
            var validationResult = new ValidationResult(
                "At least one of 'EcsClusters' or 'CloudMapNamespaces' name must be specified.",
                new[] {nameof(options.EcsClusters), nameof(options.CloudMapNamespaces)});

            validationErrors.Add(validationResult);
        }

        if (validationErrors.Count > 0)
        {
            return ValidateOptionsResult.Fail(
                validationErrors
                    .Where(ve => ve.ErrorMessage != null)
                    .Select(ve => ve.ErrorMessage)!
            );
        }

        return ValidateOptionsResult.Success;
    }
}