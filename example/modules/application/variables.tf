variable "environment" {
  description = "Name of the integration environment. For example: dev, qa, prod, etc. "
  type        = string
}

variable "application_environment" {
  # Note: In this example I'm using .NET Core application as a demo application.
  # You can replace this with any other application environment variable that suites your use case.
  description = "Name of the application configuration environment. For example: UsProduction, QaMain, etc."
  type        = string
  default     = "Development"
}

variable "vpc_id" {
  description = "VPC Id in which application is being deployed. Required for obtaining the VPC CIDR block."
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

variable "ecs_custer_id" {
  description = "Id of the ECS cluster where application service will be created."
  type        = string
}

variable "ecs_service_name" {
  description = "Name of the ECS service for the application."
  type        = string
}

variable "ecr_repository_url" {
  description = "Docker image repository URL for the application from ECR. ECR repository must allow pulls from your account."
  type        = string
}

variable "ecr_image_tag" {
  description = "Docker image tag for the application from ECR."
  type        = string
}

variable "ecs_security_group_id" {
  description = "Id of the security group for the ECS cluster. Must allow ports for your applications to communicate."
  type        = string
}

variable "container_resources" {
  description = "Resources for the container"
  type = object({
    cpu    = string
    memory = string
  })

  default = {
    cpu    = "256"
    memory = "256"
  }
}

variable "cloud_watch_log_group_name" {
  description = "Name of the CloudWatch log group for the application service."
  type        = string
}

variable "service_discovery_namespace" {
  description = "CloudMap Service discovery namespace for the application service."
  type        = object({
    id          = string
    name        = string
  })
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
}
