using System.Text;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Models;
using Serilog;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;

public static class PrometheusResponseFactory
{
    /// <summary>
    /// Method takes in a collection of discovery targets and returns an array of objects,
    /// swhich when returned as JSON, comply with the Prometheus HTTP_SD format
    /// </summary>
    /// <param name="discoveryTargets">
    /// Collection of pre-built discovery targets
    /// </param>
    /// <remarks>
    /// Read more at <a href="https://prometheus.io/docs/prometheus/latest/http_sd/#http_sd-format">Prometheus Documentation</a>
    /// </remarks>
    /// <returns></returns>
    public static PrometheusResponse Create(ICollection<DiscoveryTarget> discoveryTargets)
    {
        var response = new PrometheusResponse();

        foreach (var discoveryTarget in discoveryTargets)
        {
            foreach (var scrapeConfiguration in discoveryTarget.ScrapeConfigurations)
            {
                var staticConfig = new StaticConfigResponse
                {
                    Targets = [$"{discoveryTarget.IpAddress}:{scrapeConfiguration.Port}"],
                    Labels = new Dictionary<string, string>
                    {
                        {"__metrics_path__", scrapeConfiguration.MetricsPath},
                        {"instance", discoveryTarget.IpAddress},
                    }
                };
                // Add scrape configuration specific labele, if provided
                if (!string.IsNullOrWhiteSpace(scrapeConfiguration.Name))
                {
                    staticConfig.Labels.Add("scrape_cfg_name", scrapeConfiguration.Name);
                }

                // Add ECS specific labels
                staticConfig.Labels.Add("ecs_cluster", discoveryTarget.EcsCluster);
                staticConfig.Labels.Add("ecs_service", discoveryTarget.EcsService);
                staticConfig.Labels.Add("ecs_task", discoveryTarget.EcsTaskArn);
                staticConfig.Labels.Add("ecs_task_definition_arn", discoveryTarget.EcsTaskDefinitionArn);

                // Add optional CloudMap labels, when not empty
                staticConfig.Labels.AddLabelWithValue("cloudmap_service_name", discoveryTarget.CloudMapServiceName);
                staticConfig.Labels.AddLabelWithValue("cloudmap_service_instance_id", discoveryTarget.CloudMapServiceInstanceId);
                staticConfig.Labels.AddLabelWithValue("cloudmap_service_type", discoveryTarget.ServiceType?.ToString() ?? "");

                // Now add all the labels from the target
                foreach (var discoveryLabel in discoveryTarget.Labels)
                {
                    staticConfig.Labels.AddLabelWithValue(discoveryLabel.Name, discoveryLabel.Value);
                }

                response.Add(staticConfig);
            }
        }

        return response;
    }

    /// <summary>
    /// Adds a label to the dictionary if the value is not null or empty
    /// </summary>
    internal static void AddLabelWithValue(this Dictionary<string, string> labels, string labelName, string labelValue)
    {
        if (string.IsNullOrWhiteSpace(labelValue)) return;
        var validLabelName = labelName.ToValidPrometheusLabelName();
        if (!labels.TryAdd(validLabelName, labelValue))
        {
            Log.Debug("Failed to add label {LabelName} with value {LabelValue}", validLabelName, labelValue);
        }
    }

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
    internal static string ToValidPrometheusLabelName(this string labelName)
    {
        var filterPredicate = (char ch) => char.IsLetter(ch) || char.IsDigit(ch) || ch == '_' ? ch : '_';
        var labelNameBuilder = new StringBuilder();
        foreach (var c in labelName.Select(filterPredicate))
        {
            labelNameBuilder.Append(c);
        }

        // Ensure that the first character is a valid label name character
        if (!char.IsLetter(labelNameBuilder[0]) && labelNameBuilder[0] != '_')
        {
            labelNameBuilder.Insert(0, ['_']);
        }

        return labelNameBuilder.ToString();
    }
}