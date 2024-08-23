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
resource "aws_iam_role_policy_attachment" "cloudmap_read" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSCloudMapReadOnlyAccess"
}

resource "aws_iam_role_policy_attachment" "cloudmap_registration" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSCloudMapRegisterInstanceAccess"
}

resource "aws_iam_role_policy_attachment" "cloudwatch_full_access" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchLogsFullAccess"
}

resource "aws_iam_policy" "open_telemetry_policy" {
  name = "${local.resource_prefix}-open-telemetry-policy"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "logs:PutLogEvents",
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:DescribeLogStreams",
          "logs:DescribeLogGroups",
          "logs:PutRetentionPolicy",
          "xray:PutTraceSegments",
          "xray:PutTelemetryRecords",
          "xray:GetSamplingRules",
          "xray:GetSamplingTargets",
          "xray:GetSamplingStatisticSummaries",
          "cloudwatch:PutMetricData",
          "ec2:DescribeVolumes",
          "ec2:DescribeTags",
          "ssm:GetParameters",
          "aps:RemoteWrite",
        ]
        Effect   = "Allow"
        Resource = ["*"]
      },
    ]
  })
  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "open_telemetry_policy" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = aws_iam_policy.open_telemetry_policy.arn
}