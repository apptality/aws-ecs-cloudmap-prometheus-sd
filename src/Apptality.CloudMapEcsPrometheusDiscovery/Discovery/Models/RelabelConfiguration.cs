using System.Text.RegularExpressions;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;
using Serilog;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;

public class RelabelConfiguration
{
    /// <summary>
    /// Target label name
    /// </summary>
    public string TargetLabelName { get; init; } = null!;

    /// <summary>
    /// Replacement pattern
    /// </summary>
    public string? ReplacementPattern { get; init; }

    /// <summary>
    /// Temporary labels are not persisted in the discovery process,
    /// and are used only for re-labeling as temporary value holders.
    /// These must start with a _tmp prefix.
    /// </summary>
    /// <example>
    /// Adding prefix to all labels is the most common use case for temporary labels:
    /// <code>
    /// _tmp_p=my_awesome_app;ecs_cluster={{_tmp_p}}_{{ecs_cluster}};ecs_service={{_tmp_p}}_{{ecs_service}};
    /// </code>
    /// will result in ecs_cluster and ecs_service labels being prefixed with "my_awesome_app_",
    /// while the '_tmp_p' label will not be written to the target label.
    /// </example>
    public bool Temporary { get; set; }

    /// <summary>
    /// Based on pattern defined and labels provided, builds a new label where tokens are replaced with values
    /// from the labels.
    /// </summary>
    /// <param name="labels">
    /// Collection of labels to use for replacements in the replacement pattern
    /// </param>
    /// <remarks>
    /// Label can only be constructed if all the labels required for the replacement pattern are present.
    /// </remarks>
    /// <returns>
    /// New label or null if it cannot be built.
    /// </returns>
    public DiscoveryLabel? ToDiscoveryLabel(ICollection<DiscoveryLabel> labels)
    {
        // If no replacement pattern is provided, return null
        if (string.IsNullOrWhiteSpace(ReplacementPattern)) return null;

        // Extract token keys from the replacement pattern
        var tokens = ExtractTokenKeys(ReplacementPattern);

        // If no tokens are found - means that the replacement pattern is a static value.
        // Write a warning, because static values should be used instead.
        // I see this probably being used to not repeat the same value in multiple places,
        // and referencing it instead. Well, I hope you know what you're doing.
        if (tokens.Count == 0)
        {
            if (!TargetLabelName.ToLower().StartsWith("_tmp"))
            {
                Log.Warning(
                    """
                    Replacement pattern '{ReplacementPattern}' for target label '{TargetLabel}' is a static value.
                    If you're not using this for further processing, use '{ExtraPrometheusLabels}' configuration option instead.
                    """,
                    ReplacementPattern, TargetLabelName,
                    nameof(DiscoveryOptions.ExtraPrometheusLabels)
                );
                return null;
            }

            // Mark the label as temporary, because it's not used for anything else
            Temporary = true;

            return new DiscoveryLabel
            {
                Name = TargetLabelName.ToValidPrometheusLabelName(),
                Value = ReplacementPattern,
                Priority = DiscoveryLabelPriority.Relabel
            };
        }

        // Pre-select all keys from existing labels
        var existingLabelsLookup = labels.ToDictionary(l => l.Name.ToLower());

        // Ensure all the tokens are present in the labels
        if (!tokens.All(t => existingLabelsLookup.Keys.Contains(t)))
        {
            // If not all the tokens are present, return null, because the label cannot be built
            return null;
        }

        // Replace tokens with the values from the labels
        var newLabelValue = tokens.Aggregate(
            ReplacementPattern,
            (current, token) => current.Replace($$$"""{{{{{token}}}}}""", existingLabelsLookup[token].Value)
        );

        return new DiscoveryLabel
        {
            Name = TargetLabelName,
            Value = newLabelValue,
            Priority = DiscoveryLabelPriority.Relabel
        };

        // If not all the tokens are present, return null, because the label cannot be built
    }

    /// <summary>
    /// Extracts token keys from the replacement pattern
    /// </summary>
    /// <param name="replacementPattern">
    /// Replacement pattern to extract token keys from: "{{token1}} {{token2}} {{token3}}"
    /// </param>
    /// <returns>
    /// Returns a collection of token keys extracted from the replacement pattern in the order they appear,
    /// with duplicates removed, and all keys are lowercased.
    /// </returns>
    internal static ICollection<string> ExtractTokenKeys(string replacementPattern)
    {
        return Regex.Matches(replacementPattern, @"\{\{(\w+)\}\}")
            .Select(m => m.Groups[1].Value.ToLower())
            .Distinct()
            .ToList();
    }
}