resource "aws_iam_role" "ecs_task_role" {
  name = "${local.resource_prefix}-${local.ecs_service_name}-task-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Effect = "Allow"
      },
    ]
  })

  tags = var.tags
}

# Allow pulling images from ECR and logging to CloudWatch
resource "aws_iam_role_policy_attachment" "app_ecs_run" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# CloudMap policies for service discovery
resource "aws_iam_role_policy_attachment" "cloudmap_discovery" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSCloudMapDiscoverInstanceAccess"
}

resource "aws_iam_role_policy_attachment" "cloudmap_registration" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSCloudMapRegisterInstanceAccess"
}

# Read secrets from Secrets Manager.
# Very powerful when using with AppConfig, as you can store your configuration in Secrets Manager,
# and read it upon application startup.
resource "aws_iam_role_policy_attachment" "app_config_read_policy" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = aws_iam_policy.app_config_read_policy.arn
}

