# This is just an example of how to create a Service Discovery Service with multiple scrapable targets configurations for Prometheus.
#
# This application primary purpose is to scrape CloudMap "Service Connect" configurations,
# while supporting CloudMap "Service Discovery" configurations as a fallback
# for backwards compatibility with ECS Service Discovery. AWS ECS Service Discovery is being deprecated.
resource "aws_service_discovery_service" "service_discovery" {
  name        = "sd-${local.ecs_service_name}"

  dns_config {
    namespace_id   = var.service_discovery_namespace.id
    routing_policy = "MULTIVALUE"
    dns_records {
      ttl  = 60
      type = "A"
    }
  }

  tags = merge(var.tags, {
    # Tags allow specifyig multiple scrapable targets configurations for Prometheus.
    # Can bes set at the ECS Service level, ECS Task level, or at the CloudMap Namespace, or CloudMap Service level.
    # Below configuration will instruct "aws-ecs-cloudmap-prometheus-discovery" application
    # to scrape metrics from container on port 8080, and from the container on port 9779.
    # https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/blob/dev/src/Apptality.CloudMapEcsPrometheusDiscovery/appsettings.json#L115
    "METRICS_PORT"     = "8080"
    "METRICS_PATH"     = "/metrics"
    "METRICS_NAME"     = "/application"
    "METRICS_PORT_ECS" = "9779"
    "METRICS_PATH_ECS" = "/metrics"
    "METRICS_NAME_ECS" = "ecs-exporter"
  })
}
