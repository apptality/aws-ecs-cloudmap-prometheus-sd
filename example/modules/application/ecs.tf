resource "aws_ecs_service" "service" {
  name            = local.ecs_service_name
  cluster         = var.ecs_custer_id
  launch_type     = "FARGATE"
  task_definition = aws_ecs_task_definition.task_definition.id
  desired_count   = 0
  # Below are required to enforce a new deployment to be ready before the old one is stopped
  deployment_minimum_healthy_percent = 100
  deployment_maximum_percent         = 200

  propagate_tags          = "TASK_DEFINITION"
  enable_ecs_managed_tags = true

  network_configuration {
    subnets = var.private_subnet_ids
    security_groups = [
      var.ecs_security_group_id,
    ]
    assign_public_ip = false
  }

  service_registries {
    registry_arn   = aws_service_discovery_service.service_discovery.arn
    container_name = local.ecs_service_name
  }

  service_connect_configuration {
    enabled   = true
    namespace = var.service_discovery_namespace.name

    service {
      port_name      = "application"
      discovery_name = local.ecs_service_name
      client_alias {
        dns_name = local.ecs_service_name
        port     = 8080
      }
      timeout {
        idle_timeout_seconds        = 0
        per_request_timeout_seconds = 0
      }
    }

    log_configuration {
      log_driver = "awslogs"
      options = {
        awslogs-group         = var.cloud_watch_log_group_name
        awslogs-region        = local.region
        awslogs-stream-prefix = "${local.ecs_service_name}-service-connect"
      }
    }

  }

  #   Uncomment below to enable load balancer support for your application.
  #   You'll have to create variables & pass the list of target group ARNs to the module.
  #   dynamic "load_balancer" {
  #     for_each = var.target_group_arns
  #
  #     content {
  #       target_group_arn = load_balancer.value
  #       container_name   = local.ecs_service_name
  #       container_port   = 8080
  #     }
  #   }

  lifecycle {
    ignore_changes = [
      desired_count
    ]
  }

  tags = var.tags
}

resource "aws_ecs_task_definition" "task_definition" {
  family                   = "${local.resource_prefix}-${local.ecs_service_name}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.container_resources.cpu
  memory                   = var.container_resources.memory
  execution_role_arn       = aws_iam_role.ecs_task_role.arn
  task_role_arn            = aws_iam_role.ecs_task_role.arn

  container_definitions = jsonencode([
    {
      name      = local.ecs_service_name
      image     = "${local.ecr_repository_url}:${var.ecr_image_tag}"
      essential = true
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = local.ecs_service_name
        }
      }
      portMappings = [
        {
          name          = "application"
          appProtocol   = "http"
          containerPort = 8080
          protocol      = "tcp"
        }
      ]
      healthCheck = {
        command      = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
        interval     = 30
        timeout      = 5
        retries      = 3
        start_period = 60
      }
      environment = [
        # Example of setting environment variables for a .NET Core application,
        # as I'm using .NET Core example application in for this project.
        {
          name  = "ASPNETCORE_ENVIRONMENT"
          value = var.application_environment
        },
        {
          name  = "ASPNETCORE_URLS"
          value = "http://*:8080"
        },
        {
          name  = "AWS_REGION"
          value = local.region
        },
        {
          name  = "AWS_SECRET_NAME"
          value = aws_secretsmanager_secret.app_config.name
        }
      ]
      secrets = [
      ]
    },
    {
      name = "ecs-exporter"
      # Export container metrics: https://github.com/prometheus-community/ecs_exporter
      image     = "public.ecr.aws/awsvijisarathy/ecs-exporter:1.2"
      essential = true
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = "${local.ecs_service_name}-ecs-exporter"
        }
      }
      portMappings = [
        {
          name          = "ecs-exporter"
          appProtocol   = "http"
          containerPort = 9779
          protocol      = "tcp"
        }
      ]
    },
  ])

  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }

  tags = merge(var.tags, {
    # Tags allow specifying multiple scrapable targets configurations for Prometheus.
    # Can bes set at the ECS Service level, ECS Task level, or at the CloudMap Namespace, or CloudMap Service level.
    # Below configuration will instruct "aws-ecs-cloudmap-prometheus-discovery" application
    # to scrape metrics from container on port 8080, and from the container on port 9779.
    # https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/blob/dev/src/Apptality.CloudMapEcsPrometheusDiscovery/appsettings.json#L115
    # This duplicates the configuration in the CloudMap service resource ("aws_service_discovery_service.service_discovery")
    # just to show how to set the same configuration at the ECS Task level. These configuration are resolved in a specific order.
    # See appsettings.json for more details.
    "METRICS_PORT"      = "8080"
    "METRICS_PATH"      = "/metrics"
    "METRICS_NAME"      = "/application"
    "METRICS_PORT_ECS"  = "9779"
    "METRICS_PATH_ECS"  = "/metrics"
    "METRICS_NAME_ECS"  = "ecs-exporter"
    # This needs to alight with the "DiscoveryOptions__EcsServiceSelectorTags"
    # in the "aws-ecs-cloudmap-prometheus-discovery" application.
    # This allows to discover and scrape only those applications,
    # which you're interested in.
    "PROMETHEUS_TARGET" = "true"
  })
}
