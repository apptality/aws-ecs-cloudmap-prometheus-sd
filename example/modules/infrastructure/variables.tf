variable "environment" {
  description = "Name of the integration environment. For example: dev, qa, prod, etc. "
  type        = string
}

variable "vpc_id" {
  description = "VPC Id in which the ECS cluster will be created"
  type        = string
}

variable "public_subnet_ids" {
  description = "List of public subnet ids"
  type        = list(any)
}

variable "private_subnet_ids" {
  description = "List of private subnet ids"
  type        = list(any)
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
}
