locals {
  # Identifier of VPC in which all resources are being created.
  vpc_id = "vpc-1234567890abcdefg"
  # Name of integration environment. This is by the "purpose" (you can have many 'dev' environments)
  environment = "dev"
  # Name of the application environment. For example: aws-dev, aws-prod, etc.
  # This is used as a lookup for the source artifacts, docker container tags, etc.
  # This would also reference concrete configuration values for the application.
  application_environment = "Development"
  # List of public subnet ids. These are typically ones in which public facing services are deployed.
  # It is recommended to have at least two public subnets in different availability zones for high availability.
  public_subnet_ids = [
    "subnet-12345678901234567", # vpc-public-subnet-1
    "subnet-23456789012345679", # vpc-public-subnet-2
  ]
  # List of private subnet ids. These are typically ones in which private services are deployed (DBs, internal services, etc.)
  # It is recommended to have at least two private subnets in different availability zones for high availability.
  private_subnet_ids = [
    "subnet-34567890123456790", # vpc-private-subnet-1
    "subnet-45678901234567901", # vpc-private-subnet-2
  ]
  # Any additional tags to be added to the resources.
  tags = {}
}