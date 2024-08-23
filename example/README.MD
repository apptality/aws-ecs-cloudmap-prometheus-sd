# AWS ECS & CloudMap Prometheus Discovery - End-to-End Example
🚧 🚧 🚧

This example is a work in progress.
This note will be removed when tested in AWS.
This though should be a solid starting point for you to build upon, with no major missing parts of the infrstructure.

---

This is a complete example of what it takes to run an observable
infrastructure in AWS ECS.

This example uses [Terraform](https://www.terraform.io/) to provision the infrastructure,
so some familiarity with Terraform is necessary, but not strongly required.

## Structure

`~/environments` - this is where you would store your environment configurations.
In this example I have a single configuration for `dev`.

`~/modules` - repeatable infrastructure constructs that can be used across multiple environments.

Under `~/modules` you will find the following:
* `infrastructure` - this module creates shared resources in which your applications are run.
This should typically be declared once per environment.
* `application` - this is the ECS application module that will create the ECS service, task definition.
You can have as many applications as you want.
* `monitoring` - this module creates the monitoring resources for your applications.

This example expects you to already have a VPC with public and private subnets defined.

## Setup
1. Install Terraform so it is available in your PATH.

   Reload/restart of your shell may be required.
1. Navigate to the `~/examples/environments/dev` directory.
1. Paste AWS credentials in your shell for the target environment
1. Run `terraform init`
1. Run `terraform plan -out plan.tfplan`
1. Inspect the plan and confirm it looks good
1. Run `terraform apply plan.tfplan` to apply the changes

When you're done, remember to clean up:
1. Run `terraform destroy`

> **Note:** This example is semi-production-ready. It is meant to be a starting point for you to build upon.