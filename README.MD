# AWS ECS & CloudMap Prometheus Discovery

[![Build and Publish Docker Images](https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/actions/workflows/docker.yml/badge.svg?branch=dev)](https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/actions/workflows/docker.yml)
[![License: MIT](https://cdn.prod.website-files.com/5e0f1144930a8bc8aace526c/65dd9eb5aaca434fac4f1c34_License-MIT-blue.svg)](/LICENSE)
![Docker Image Version](https://img.shields.io/docker/v/apptality/aws-ecs-cloudmap-prometheus-discovery?logo=docker)
![Docker Image Size (tag)](https://img.shields.io/docker/image-size/apptality/aws-ecs-cloudmap-prometheus-discovery/0.1.0-alpha.30?logo=docker)


🚧 🚧 🚧 This repo is still work in progress and is subject to change.

## Overview

This application facilitates discovery of ECS and/or CloudMap resources,
specifically tailored to be compatible with [Prometheus HTTP Service Discovery](https://prometheus.io/docs/prometheus/latest/http_sd/#writing-http-service-discovery).

This solution leverages AWS API to dynamically discover:

* [ECS Clusters](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/clusters.html), related ECS Services, and ECS Tasks
* [CloudMap Namespaces](https://docs.aws.amazon.com/cloud-map/latest/dg/working-with-namespaces.html), registered Service Connect and Service Discovery services, and their instances

making it easier for Prometheus to monitor these services without hardcoding their addresses or ports, or using any kind of file-based service discovery.

## Features

Below is a high level overview of what application is capable of. For the full list of supported configuration parameters and how to override these using [Docker Environment Variables](https://docs.docker.com/reference/cli/docker/container/run/#env) please refer to [appsettings.json](src/Apptality.CloudMapEcsPrometheusDiscovery/appsettings.json)

* **Prometheus Compatibility**: By default, exposes `9001:/prometheus-targets` endpoint which response is compatible with [<http_sd_config>](https://prometheus.io/docs/prometheus/latest/http_sd/#writing-http-service-discovery) of Prometheus configuration file, as well as with [OpenTelemetry Prometheus Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/main/receiver/prometheusreceiver/README.md) (see example below).

* **ECS Discovery**: Supports discovery of ECS clusters, services, running tasks
  * Supply `EcsClusters` as semicolon separated string of ECS clusters to include: `"cluster1;cluster2;"`
  * Supports filtering of ECS Services that should be included in the Prometheus response based on Resource Tags (see `EcsServiceSelectorTags` configuration property)
  * Supports filtering to control which tags should be included as labels in the response (see `EcsTaskTags`, `EcsServiceTags` configuration properties)
* **AWS CloudMap Integration**: Supports discovery of CloudMap namespaces, services and instances
  * Supply `CloudMapNamespaces` as semicolon separated string of CloudMap namespaces to include: `"namespace1;"`
  * Supports filtering of CloudMap Services that should be included in the Prometheus response based on Resource Tags (see `CloudMapServiceSelectorTags` configuration property)
  * Supports filtering to control which tags should be included as labels in the response (see `CloudMapServiceTags`, `CloudMapNamespaceTags` configuration properties)
* **Scalable and Flexible**: Built to be scalable and flexible, adapting to changes in the service infrastructure
  * Allows having any number of port-metrics pairs per each running ECS Task (useful when your ECS Task Definition defines multiple containers, which also expose ports and metrics endpoint). See `MetricsPathPortTagPrefix` configuration property for more details.
  * Allows supplying static set of labels to be added to every Prometheus target (see `ExtraPrometheusLabels` configuration property)

Only one of `EcsClusters` or `CloudMapNamespaces` can be provided for application to work. If you would like to include both [Service Connect](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html) and [Service Discovery](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html) targets, `CloudMapNamespaces` must be supplied (with or without `EcsClusters`). When **both parameters** are supplied, the end result is only an intersection of targets that exist in ECS clusters supplied and CloudMap namespaces provided.

> **Permissions**: IAM permissions are required to discover ECS clusters (`ecs:Get*`, `ecs:List*`, `ecs:Describe*`) and CloudMap namespaces (`servicediscovery:Get*`, `servicediscovery:List*`, `servicediscovery:Discover*`, `route53:Get*`)

## Usage Example

> 🚧 Documentation is still in progress, it'll be finished in next week or so.

If anyone is willing to give this a trial - please refer to [appsettings.json](src/Apptality.CloudMapEcsPrometheusDiscovery/appsettings.json) for complete list of supported configuration options.

Example run command:

```bash
  docker run --rm \
    # ** REQUIRED PARAMETERS **
    # At least one of "EcsClusters" or "CloudMapNamespaces" must be provided.
    -e DiscoveryOptions__EcsClusters="<cluster1>;<cluster2>" \
    -e DiscoveryOptions__CloudMapNamespaces="<namespace1>;" \
    # If running outside of AWS - pass credentials explicitly
    # If running in AWS - use Task IAM Role.
    # IMPORTANT: Credentials need to have permissions to describe ECS cluster(optionally CloudMap namespaces)
    -e AWS_REGION="us-west-2" \
    -e AWS_ACCESS_KEY_ID="<key>" \
    -e AWS_SECRET_ACCESS_KEY="<secret>" \
    -e AWS_SESSION_TOKEN="<session_token>" \
    # ** OPTIONAL PARAMETERS **
    # Will only include those services, which have 'prometheus_target' tag set to 'true'.
    # Leave blank to include all services in cluster(s)
    -e DiscoveryOptions__EcsServiceSelectorTags="prometheus_target=true;" \
    # Same as above, but for filtering out CloudMap services
    # Leave blank ot include all services in namespace(s)
    -e DiscoveryOptions__CloudMapServiceSelectorTags="prometheus_target=true;" \
    # Semicolon separated string of tag names to include in the service discovery response as metadata.
    # Supports glob pattern matching using * and ?.
    -e DiscoveryOptions__EcsTaskTags="*" \
    -e DiscoveryOptions__EcsServiceTags="*" \
    -e DiscoveryOptions__CloudMapServiceTags="AmazonECS*;" \
    -e DiscoveryOptions__CloudMapNamespaceTags="*" \
    # Semicolon separated string of labels to include in the service discovery response as metadata.
    # Will be added to all discovered targets.
    -e DiscoveryOptions__ExtraPrometheusLabels="custom_static_tag=my-static-tag;" \
    # Tag prefix to identify metrics port, [path | "/metrics"], [name | ""] triplets.
    # Please refer to the documentation to learn more.
    -e DiscoveryOptions__MetricsPathPortTagPrefix="METRICS_" \
    # Instructs .NET application to listen on 9001 inside the container
    -e ASPNETCORE_URLS="http://*:9O01" \
    # ** DOCKER **
    -p 9001:9001 \
    apptality/aws-ecs-cloudmap-prometheus-discovery:<version>
```

Example output:

```json
[
  {
    "targets": [
      "10.200.10.200:8080"
    ],
    "labels": {
      "__metrics_path__": "/metrics",
      "instance": "10.200.10.200",
      "ecs_cluster": "my-ecs-cluster",
      "ecs_service": "my-fargate-application",
      "ecs_task": "arn:aws:ecs:us-west-2:123456789012:task/my-ecs-cluster/c88nc14799fa46d794c1899612061h3s",
      "ecs_task_definition_arn": "arn:aws:ecs:us-west-2:123456789012:task-definition/my-fargate-application:2",
      "cloudmap_service_name": "service-application",
      "cloudmap_service_instance_id": "kj2pc14799fa46d794c189961206k00q",
      "cloudmap_service_type": "ServiceConnect",
      "custom_resource_tag_environment": "dev",
      "custom_resource_tag_component": "my-cool-application",
      "custom_static_tag": "my-static-tag",
      "AmazonECSManaged": "true"
    }
  },
  ...
]
```

Plays nice with [OpenTelemetry receivers config](https://opentelemetry.io/docs/collector/configuration/#receivers):

Considering your ECS Task Definition for discovery looks like this:

```terraform
resource "aws_ecs_task_definition" "task_definition" {
  family                   = "${local.resource_prefix}-${local.ecs_name}"
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
      name      = "${local.ecs_name}-config-reloader"
      image     = "apptality/aws-ecs-cloudmap-prometheus-discovery:0.1.0-alpha.29"
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
        {
          name  = "AWS_REGION"
          value = local.region
        },
        ... REST OF APPLICATION CONFIGURATION ...
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = "${local.ecs_name}-config-reloader"
        }
      }
    },
    {
      name      = "${local.ecs_name}-collector"
      image     = "public.ecr.aws/aws-observability/aws-otel-collector:latest"
      cpu       = 384
      memory    = 896
      essential = true
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = var.cloud_watch_log_group_name
          awslogs-region        = local.region
          awslogs-stream-prefix = "${local.ecs_name}-collector"
        }
      }
      secrets = [
        {
          # Contents of "otel-collector-config.yaml" below stored in SSM
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
          containerName = "${local.ecs_name}-config-reloader"
          condition     = "START"
        }
      ]
    }
  ])
}
```

Your OTEL configuration may look something similar to:

```yaml
# otel-collector-config.yaml
receivers:
  prometheus:
    config:
      global:
        scrape_interval: 15s
        scrape_timeout: 10s
      scrape_configs:
        - job_name: ecs_services
          http_sd_configs:
            - url: http://localhost:9001/prometheus-targets
              refresh_interval: 10s
exporters:
  prometheusremotewrite:
    endpoint: https://aps-workspaces.${REGION}.amazonaws.com/workspaces/${WORKSPACE}/api/v1/remote_write
    resource_to_telemetry_conversion:
      enabled: true
    auth:
      authenticator: sigv4auth
extensions:
  sigv4auth:
    service: aps
    region: ${REGION}
  health_check: null
  pprof:
    endpoint: ":1888"
  zpages:
    endpoint: ":55679"
service:
  extensions:
    - sigv4auth
    - pprof
    - zpages
    - health_check
  pipelines:
    metrics:
      receivers: [prometheus]
      exporters: [prometheusremotewrite]
```

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues to discuss potential improvements or features.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
