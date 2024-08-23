variable "environment" {
  description = "Name of the integration environment. For example: dev, qa, prod, etc. "
  type        = string
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

variable "service_discovery_namespace" {
  description = "CloudMap Service discovery namespace for the application service."
  type        = object({
    id          = string
    name        = string
  })
}

variable "ecs_custer_id" {
  description = "Id of the ECS cluster where application service will be created."
  type        = string
}

variable "ecs_cluster_name" {
  description = "Name of the ECS cluster where application service will be created."
  type        = string
}

variable "docker_image_otel_collector" {
  # See: https://aws-otel.github.io/docs/setup/ecs
  description = "Docker image for the AWS OTel Collector"
  type        = string
  default     = "public.ecr.aws/aws-observability/aws-otel-collector:latest"
}

variable "docker_image_prometheus_sd" {
  # See: https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd
  description = "Docker image for discovering Prometheus targets"
  type        = string
  default     = "apptality/aws-ecs-cloudmap-prometheus-discovery:latest"
}

variable "cloud_watch_log_group_name" {
  description = "Name of the CloudWatch log group for the application service."
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
}
