# Stores OTEL collector configuration in SSM Parameter Store, so that it can be accessed by the ECS task definition.
# For more information, please refer to the following links:
# * https://github.com/aws-samples/prometheus-for-ecs/blob/main/deploy-adot/README.md
# * https://aws-otel.github.io/docs/introduction
resource "aws_ssm_parameter" "ecs_opentelemtry_config" {
  name = "/${local.resource_prefix}/opentelemetry-otel-collector-config"
  type = "String"
  value = templatefile("${path.module}/assets/otel-collector-config.yaml.tmpl", {
    REGION    = local.region
    WORKSPACE = aws_prometheus_workspace.amp_workspace.id
  })
  tags = var.tags
}
