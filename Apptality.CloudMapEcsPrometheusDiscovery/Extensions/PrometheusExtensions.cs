using System.Text;
using Apptality.CloudMapEcsPrometheusDiscovery.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Extensions;

/// <summary>
/// Contains extension methods for Prometheus
/// </summary>
public static class PrometheusExtensions
{
    /// <summary>
    /// Converts arbitrary string to a valid Prometheus label name
    /// </summary>
    /// <param name="labelName">Label name to convert</param>
    /// <remarks>
    /// <b>labelname</b>: a string matching the regular expression [a-zA-Z_][a-zA-Z0-9_]*.
    /// Any other unsupported character in the source label should be converted to
    /// an underscore. For example, the label app.kubernetes.io/name should be written as app_kubernetes_io_name.
    /// You can read about label name requirements in the <a href="https://prometheus.io/docs/prometheus/latest/configuration/configuration/#labelname">Prometheus documentation</a>
    /// </remarks>
    /// <returns>
    /// Properly formatted Prometheus label name
    /// </returns>
    public static string ToValidPrometheusLabelName(this string labelName)
    {
        var filterPredicate = (char ch) => char.IsLetter(ch) || char.IsDigit(ch) || ch == '_' ? ch : '_';
        var labelNameBuilder = new StringBuilder();
        labelNameBuilder.Append(labelName.Select(filterPredicate));

        // Ensure that the first character is a valid label name character
        if (!char.IsLetter(labelNameBuilder[0]) || labelNameBuilder[0] != '_')
        {
            labelNameBuilder.Insert(0, ['_']);
        }

        return labelNameBuilder.ToString();
    }

    /// <summary>
    /// Converts extra prometheus labels supplied via configuration to a list of objects
    /// </summary>
    /// <param name="extraPrometheusLabels">Semicolon separated string of labels to include in the service discovery response as metadata.</param>
    /// <remarks>
    /// For example, "environment=dev;region=us-west-2"
    /// If a value needs to have a semicolon, or equals sign, it must be prefixed using %.
    /// For example, "eval_expression=a%=%=b;components=a%;b%;;"
    /// would be parsed as "eval_expression=a==b;components=a;b;"
    /// </remarks>
    /// <returns>
    /// List of objects representing labels and with their values.
    /// </returns>
    public static List<PrometheusLabel> ToPrometheusLabels(this string extraPrometheusLabels)
    {
        const string equalReplacementSource = "%=";
        const string equalReplacementToken = "{equal_sign}";
        const string semicolonReplacementSource = "%;";
        const string semicolonReplacementToken = "{semicolon}";

        List<PrometheusLabel> labels = [];

        // First, tokenize source string
        var tokenizedLabelsSource = extraPrometheusLabels
            .Replace(equalReplacementSource, equalReplacementToken)
            .Replace(semicolonReplacementSource, semicolonReplacementToken);

        foreach (var keyValuePair in tokenizedLabelsSource.Split(';'))
        {
            // Skip empty pairs
            if (string.IsNullOrWhiteSpace(keyValuePair)) continue;

            var labelValuePair = keyValuePair.Split('=');

            // Ensure we have a valid key-value pair
            if (labelValuePair.Length != 2) continue;

            var labelName = Reconstruct(labelValuePair[0]).ToValidPrometheusLabelName();
            var labelValue = Reconstruct(labelValuePair[1]);

            labels.Add(new PrometheusLabel(labelName, labelValue));
        }

        return labels;

        // Reconstructs tokenized string back to having original values
        string Reconstruct(string tokenizedSource) =>
            tokenizedSource
                .Replace(equalReplacementToken, "=")
                .Replace(equalReplacementToken, ";");
    }
}