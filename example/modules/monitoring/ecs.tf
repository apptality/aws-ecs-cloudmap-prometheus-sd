resource "aws_ecs_service" "service" {
  name            = local.ecs_service_name
  cluster         = var.ecs_custer_id
  launch_type     = "FARGATE"
  task_definition = aws_ecs_task_definition.task_definition.id
  desired_count   = 0
  # Required to SSH into the container
  enable_execute_command = true

  # Below are required to enforce a new deployment to be ready before the old one is stopped
  deployment_minimum_healthy_percent = 0
  deployment_maximum_percent         = 100

  network_configuration {
    subnets = var.private_subnet_ids
    security_groups = [
      aws_security_group.security_group.id
    ]
    assign_public_ip = false
  }

  lifecycle {
    ignore_changes = [
      desired_count
    ]
  }

  tags = var.tags
}

# Documentation can be found here:
# https://aws-otel.github.io/docs/setup/ecs
resource "aws_ecs_task_definition" "task_definition" {
  family                   = "${local.resource_prefix}-${local.ecs_service_name}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = aws_iam_role.ecs_task_role.arn
  task_role_arn            = aws_iam_role.ecs_task_role.arn

  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }

  container_definitions = jsonencode([
    {
      name      = "${local.ecs_service_name}-prometheus-sd"
      image     = var.docker_image_prometheus_sd
      cpu       = 128
      memory    = 128
      essential = true
      portMappings = [
        {
          containerPort = 9001
          protocol      = "tcp"
        }
      ]
      environment = [
        # This is just a small subset of the environment variables that can be set.
        # See "appsettings.json" for complete set of parameters
        {
          name  = "DiscoveryOptions__EcsClusters"
          value = var.ecs_cluster_name
        },
        {
          name  = "DiscoveryOptions__EcsServiceSelectorTags"
          value = "PROMETHEUS_TARGET=true"
        },
        {
          name  = "AWS_REGION"
          value = local.region
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = "${local.ecs_service_name}-prometheus-sd"
        }
      }
    },
    {
      name      = "${local.ecs_service_name}-collector"
      image     = var.docker_image_otel_collector
      cpu       = 384
      memory    = 896
      essential = true
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = "${local.ecs_service_name}-collector"
        }
      }
      secrets = [
        {
          name      = "AOT_CONFIG_CONTENT"
          valueFrom = aws_ssm_parameter.ecs_opentelemtry_config.name
        }
      ]
      portMappings = [
        {
          containerPort = 2000
          protocol      = "udp"
        }
      ]
      dependsOn = [
        {
          containerName = "${local.ecs_service_name}-prometheus-sd"
          condition     = "START"
        }
      ]
    }
  ])

  tags = var.tags
}

resource "aws_security_group" "security_group" {
  name        = "${local.resource_prefix}-open-telemetry"
  description = "Security group for performing actions related to open telemetry workloads"

  vpc_id = var.vpc_id
  ingress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = [local.network_cidr]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = var.tags
}

