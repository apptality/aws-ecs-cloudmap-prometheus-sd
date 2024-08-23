output "ecs_cluster_id" {
  value = aws_ecs_cluster.cluster.id
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.cluster.name
}

output "ecs_security_group_id" {
  value = aws_security_group.cluster.id
}

output "cloud_watch_log_group_name" {
  value = aws_cloudwatch_log_group.app.name
}

output "service_discovery_namespace" {
  value = {
    id          = aws_service_discovery_private_dns_namespace.app.id
    name        = aws_service_discovery_private_dns_namespace.app.name
  }
}