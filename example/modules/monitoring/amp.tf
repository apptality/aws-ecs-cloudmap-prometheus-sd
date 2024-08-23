# I suggest using "Amazon Managed Service for Prometheus" to store metrics exported from ECS
# Read more here:
# * https://aws-otel.github.io/docs/getting-started/prometheus-remote-write-exporter/ecs
# * https://docs.aws.amazon.com/prometheus/latest/userguide/what-is-Amazon-Managed-Service-Prometheus.html

# Create an AMP workspace
resource "aws_prometheus_workspace" "amp_workspace" {
  alias = "${local.resource_prefix}-workspace"
  tags  = var.tags
}
