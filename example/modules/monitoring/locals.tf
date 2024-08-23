data "aws_caller_identity" "current" {}
data "aws_region" "current" {}
data "aws_vpc" "default" {
  id = var.vpc_id
}

locals {
  region             = data.aws_region.current.name
  account_id         = data.aws_caller_identity.current.account_id
  resource_prefix    = "mycorp-${var.environment}"
  network_cidr       = data.aws_vpc.default.cidr_block
  ecs_service_name   = "aws-otel"  # https://aws-otel.github.io/docs/introduction
}
