resource "aws_cloudwatch_log_group" "app" {
  name              = "${local.resource_prefix}/my-app"
  retention_in_days = 30
  tags              = var.tags
}
