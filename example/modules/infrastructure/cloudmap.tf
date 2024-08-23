resource "aws_service_discovery_private_dns_namespace" "app" {
  name        = "application.${var.environment}"
  description = "Namespace for ECS services to communicate"
  vpc         = var.vpc_id
  tags        = var.tags
}
