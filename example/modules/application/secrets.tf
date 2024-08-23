
resource "aws_secretsmanager_secret" "app_config" {
  name = "${local.resource_prefix}/${local.ecs_service_name}"

  # Ensure the secret is removed from the recycle bin immediately
  recovery_window_in_days = 0

  tags = var.tags
}

resource "aws_iam_policy" "app_config_read_policy" {
  name = "${local.resource_prefix}-${local.ecs_service_name}-config-read"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "secretsmanager:GetSecretValue",
          "kms:Decrypt"
        ]
        Effect = "Allow"
        Resource = [
          aws_secretsmanager_secret.app_config.id,
          "arn:aws:kms:${local.region}:${local.account_id}:key/aws/secretsmanager"
        ]
      },
    ]
  })
}
