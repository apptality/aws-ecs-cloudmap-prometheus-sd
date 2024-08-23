terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.59"
      configuration_aliases = [
        aws
      ]
    }
  }
}
