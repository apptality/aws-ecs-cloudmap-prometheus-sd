resource "aws_ecs_cluster" "cluster" {
  name = "${local.resource_prefix}-cluster"

  service_connect_defaults {
    namespace = aws_service_discovery_private_dns_namespace.app.arn
  }

  tags = var.tags
}

resource "aws_security_group" "cluster" {
  name   = "${aws_ecs_cluster.cluster.name}-sg"
  vpc_id = var.vpc_id

  ingress {
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = [local.network_cidr]
    description = "application"
  }

  # For example, if you have a database running in RDS
  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [local.network_cidr]
    description = "database"
  }

  # .. Any other ports your applications communicate on...

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}
