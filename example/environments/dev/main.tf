# 1. Bootstrap shared infrastructure
module "infrastructure" {
  source = "../../modules/infrastructure"

  environment        = local.environment
  vpc_id             = local.vpc_id
  public_subnet_ids  = local.public_subnet_ids
  private_subnet_ids = local.private_subnet_ids
  tags               = local.tags
}

# 2. Create as many services as needed.
# This template below creates a service called "webservice-backend"
module "app-webservice-backend" {
  source = "../../modules/application"

  environment                 = local.environment
  application_environment     = local.application_environment
  vpc_id                      = local.vpc_id
  public_subnet_ids           = local.public_subnet_ids
  private_subnet_ids          = local.private_subnet_ids
  ecs_custer_id               = module.infrastructure.ecs_cluster_id
  ecs_service_name            = "webservice-backend"
  ecr_repository_url          = "123456789012.dkr.ecr.us-west-2.amazonaws.com/webservice-backend"
  ecr_image_tag               = "latest"
  ecs_security_group_id       = module.infrastructure.ecs_security_group_id
  container_resources         = { cpu = "512", memory = "1024" }
  cloud_watch_log_group_name  = module.infrastructure.cloud_watch_log_group_name
  service_discovery_namespace = module.infrastructure.service_discovery_namespace
  tags                        = local.tags
}

# 3. Monitoring
# This service is responsible for discovering Prometheus targets in the ECS cluster
# Once this is deployed and running - all you have to do is to hook this up to your Grafana:
# https://docs.aws.amazon.com/prometheus/latest/userguide/AMP-onboard-query-standalone-grafana.html
module "monitoring" {
  source = "../../modules/monitoring"

  environment                 = local.environment
  vpc_id                      = local.vpc_id
  public_subnet_ids           = local.public_subnet_ids
  private_subnet_ids          = local.private_subnet_ids
  ecs_custer_id               = module.infrastructure.ecs_cluster_id
  ecs_cluster_name            = module.infrastructure.ecs_cluster_name
  cloud_watch_log_group_name  = module.infrastructure.cloud_watch_log_group_name
  service_discovery_namespace = module.infrastructure.service_discovery_namespace
  tags                        = local.tags
}
