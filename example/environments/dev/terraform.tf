terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.59"
    }
  }
}

provider "aws" {
  region = "us-west-2"

  default_tags {
    tags = {
      terraform   = true
      component   = "my-app"
      environment = "dev"
    }
  }
}