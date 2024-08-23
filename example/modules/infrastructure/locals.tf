data "aws_caller_identity" "current" {}
data "aws_region" "current" {}
data "aws_vpc" "default" {
  id = var.vpc_id
}

locals {
  region          = data.aws_region.current.name
  account_id      = data.aws_caller_identity.current.account_id
  network_cidr    = data.aws_vpc.default.cidr_block
  resource_prefix = "mycorp-${var.environment}"
}
