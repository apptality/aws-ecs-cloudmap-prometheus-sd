# AWS ECS & CloudMap Prometheus Discovery

[![Build](https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/actions/workflows/docker.yml/badge.svg?branch=develop)](https://github.com/apptality/aws-ecs-cloudmap-prometheus-sd/actions/workflows/docker.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue)](/LICENSE)
[![Docker Image Version](https://img.shields.io/docker/v/apptality/aws-ecs-cloudmap-prometheus-discovery?logo=docker&label=Latest%20Version)](https://hub.docker.com/r/apptality/aws-ecs-cloudmap-prometheus-discovery)
[![Docker Image Size (tag)](https://img.shields.io/docker/image-size/apptality/aws-ecs-cloudmap-prometheus-discovery/latest?logo=docker&label=Image%20Size)](https://hub.docker.com/r/apptality/aws-ecs-cloudmap-prometheus-discovery/tags)

## Overview

This application facilitates discovery of ECS and/or CloudMap resources and compatible with [Prometheus HTTP Service Discovery](https://prometheus.io/docs/prometheus/latest/http_sd/#writing-http-service-discovery).

Application leverages AWS API to dynamically discover:

* [ECS Clusters](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/clusters.html), related ECS Services, and ECS Tasks
* [CloudMap Namespaces](https://docs.aws.amazon.com/cloud-map/latest/dg/working-with-namespaces.html), registered Service Connect and Service Discovery services, and their instances

making it easier for Prometheus to monitor these services without hardcoding their addresses and ports, or using any kind of [file-based discovery](https://prometheus.io/docs/guides/file-sd/) (thus can be run as a standalone application: as it's own AWS ECS Service, for example).

![AWS ECS & CloudMap Prometheus Discovery](/docs/diagram.svg)

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
  * Supports rich mechanism for re-labling, so you don't have to fix your Grafana dashboards. See `RelabelConfigurations` configuration property for more details.
  * Exposes health check endpoint: `/health`

At least one of `EcsClusters` or `CloudMapNamespaces` must be provided for application to work. If you would like to include both [Service Connect](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html) and [Service Discovery](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html) targets, `CloudMapNamespaces` must be specified (with or without `EcsClusters`). When **both parameters** are specified, the end result is only an intersection of targets that exist in ECS clusters and CloudMap namespaces provided.

> **Permissions**: IAM permissions are required to discover ECS clusters (`ecs:Get*`, `ecs:List*`, `ecs:Describe*`) and CloudMap namespaces (`servicediscovery:Get*`, `servicediscovery:List*`, `servicediscovery:Discover*`, `route53:Get*`)


## Usage Example

For the full example of running this in AWS, please navigate to [/example](/example) folder. Integrates with [OpenTelemetry receivers config](https://opentelemetry.io/docs/collector/configuration/#receivers).


Refer to [appsettings.json](src/Apptality.CloudMapEcsPrometheusDiscovery/appsettings.json) for all supported configuration options.

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
    -e AWS_REGION="us-west-1" \
    -e AWS_ACCESS_KEY_ID="<key>" \
    -e AWS_SECRET_ACCESS_KEY="<secret>" \
    -e AWS_SESSION_TOKEN="<session_token>" \
    # ** OPTIONAL PARAMETERS **
    # Will only include those services, which have 'prom_scrape_target' tag set to 'yes'.
    # Leave blank to include all services in cluster(s)
    -e DiscoveryOptions__EcsServiceSelectorTags="prom_scrape_target=yes;" \
    # Same as above, but for filtering out CloudMap services
    # Leave blank ot include all services in namespace(s)
    -e DiscoveryOptions__CloudMapServiceSelectorTags="prom_scrape_target=yes;" \
    # Semicolon separated string of tag keys to include in the service discovery response as metadata.
    # Supports glob pattern matching using * and ?.
    -e DiscoveryOptions__EcsTaskTags="*" \
    -e DiscoveryOptions__EcsServiceTags="*" \
    -e DiscoveryOptions__CloudMapServiceTags="AmazonECS*;" \
    -e DiscoveryOptions__CloudMapNamespaceTags="*" \
    # Semicolon separated string of labels to include in the service discovery response as metadata.
    # Will be added to all discovered targets.
    -e DiscoveryOptions__ExtraPrometheusLabels="custom_static_tag=my-static-tag;" \
    # Tag prefix to identify metrics port, [path | "/metrics"], [name | ""] triplets.
    # Please refer to the configuration options to learn more.
    -e DiscoveryOptions__MetricsPathPortTagPrefix="METRICS_" \
    # Add new or modify existing labels in the response using token replacements,
    # to prevent the need of modifying your Grafana dashboards.
    # Please refer to the configuration options to learn more.
    -e DiscoveryOptions__RelabelConfigurations="cluster_and_service={{_sys_ecs_cluster}}-{{_sys_ecs_service}}" \
    # Instructs .NET application to listen on 9001 inside the container
    -e ASPNETCORE_URLS="http://*:9O01" \
    # ** DOCKER **
    -p 9001:9001 \
    apptality/aws-ecs-cloudmap-prometheus-discovery:latest
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
      "scrape_target_name": "app",
      "__meta_cloudmap_service_instance_id": "c88nc14799fa46d794c1899612061h3s",
      "__meta_cloudmap_service_name": "service-app",
      "__meta_cloudmap_service_type": "ServiceConnect",
      "__meta_ecs_cluster": "my-ecs-cluster",
      "__meta_ecs_service": "my-fargate-application",
      "__meta_ecs_task": "arn:aws:ecs:us-west-2:123456789012:task/my-ecs-cluster/c88nc14799fa46d794c1899612061h3s",
      "__meta_ecs_task_definition": "arn:aws:ecs:us-west-2:123456789012:task-definition/my-fargate-application:2",
      "_sys_cloudmap_service_instance_id": "c88nc14799fa46d794c1899612061h3s",
      "_sys_cloudmap_service_name": "my-fargate-application",
      "_sys_cloudmap_service_type": "ServiceConnect",
      "_sys_ecs_cluster": "my-ecs-cluster",
      "_sys_ecs_service": "my-fargate-application",
      "_sys_ecs_task": "arn:aws:ecs:us-west-2:123456789012:task/my-ecs-cluster/c88nc14799fa46d794c1899612061h3s",
      "_sys_ecs_task_definition": "arn:aws:ecs:us-west-2:123456789012:task-definition/my-fargate-application:2",
      "prom_scrape_target": "yes",
      "AmazonECSManaged": "true",
      "custom_static_tag": "my-static-tag",
      "cluster_and_service": "my-ecs-cluster-my-fargate-application"
    }
  },
  ...
]
```

## Response Structure

Response is returned in [HTTP_SD format](https://prometheus.io/docs/prometheus/latest/http_sd/#http_sd-format) compatible format:

```json
[
  {
    "targets": [ "<host>", ... ],
    "labels": {
      "<labelname>": "<labelvalue>", ...
    }
  },
  ...
]
```

### Success

Success response is returned with `application/json` HTTP Content Type, and `200` HTTP status code.

Some clarification on labels returned:

* **scrape_target_name** - when any of resource contains `METRICS_NAME_` AWS Resource Tag - this label will have tag value. ECS Tasks can contain multiple containers you would like to scrape from, this label being present helps to denote between such containers when running PromQL queries.
* **__meta** - labels starting with this prefix are meta labels, and are not included into the resulting set stored Prometheus, but can be used for [re-labeling](https://prometheus.io/docs/prometheus/latest/configuration/configuration/#relabel_config).
* **_sys** - labels starting with this prefix are generated out of AWS Resources properties (ECS, CloudMap). They are prefixed as such in order to prevent conflict with your existing infrastructure configuration, and combined with `RelabelConfigurations` enable powerful manipulations of the resulting labels set.

Anything else returned is either inferred from AWS Resource Tags specified via selectors (`EcsTaskTags`, `EcsServiceTags`, `CloudMapServiceTags`, `CloudMapNamespaceTags`), supplied via `ExtraPrometheusLabels`, or product of `RelabelConfigurations` configurations.

Please note, that tags are resolved from AWS resource in the following order:

`ECS Task` > `ECS Service` > `CloudMap Service` > `CloudMap Namespace`

meaning that if ECS Service has tag `"MyCustomTag=EcsService"` and CloudMap Service has the same tag, but with different value: `"MyCustomTag=CloudMap"`  - the resulting scrape target label will have value of the former:

```json
[
  {
    "targets": [ "<host>", ... ],
    "labels": {
      "MyCustomTag": "EcsService", ...
    }
  },
  ...
]

```

### Error

When application runs into an error, response is returned with `500` HTTP status code:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

You'll need to investigate server logs for exception details:

```log
2024-08-20 21:22:23.242 -03:00 [ERR] HTTP GET /prometheus-targets responded 500 in 99.9711 ms
2024-08-20 21:22:23.243 -03:00 [ERR] An unhandled exception has occurred while executing the request.
Microsoft.Extensions.Options.OptionsValidationException: At least one of 'EcsClusters' or 'CloudMapNamespaces' name must be specified.
   at Microsoft.Extensions.Options.OptionsFactory`1.Create(String name)
...
```

## Storing this image in ECR

`~/.scripts/dockerhub-to-ecr.sh` script is intended for users who need control over their Docker images in AWS ECR, allowing to pull an image from DockerHub, re-tag it, and push it to an ECR repository with customizable options for platform and tagging.

Run the script specifying your ECR URL as the first positional parameter:

```bash
# <paste in your AWS credential>

cd ./scripts

chmod +x dockerhub-to-ecr.sh

./dockerhub-to-ecr.sh <target_ecr_repository_url> \
    # Defaults to 'apptality/aws-ecs-cloudmap-prometheus-discovery:latest'
    [source_dockerhub_image_url] \
    # Specify explicit value of image tag you want to label your ECR image with.
    # By default, same tag as on source image will be applied.
    [target_ecr_repository_tag (Default: same as source)] \
    # Specify docker platform to re-push. Amd64/Arm64 available.
    [docker_image_platform: 'linux/amd64' (Default) | 'linux/arm64']
```

Example:

```bash
./dockerhub-to-ecr.sh 123456789012.dkr.ecr.us-west-2.amazonaws.com/tools/ecs-sd
```

The above script will:

1. Authenticate with AWS ECR region `us-west-2`
1. Pull `linux/amd64` platform of `apptality/aws-ecs-cloudmap-prometheus-discovery:latest` locally
1. Re-tag it to `123456789012.dkr.ecr.us-west-2.amazonaws.com/tools/ecs-sd:latest`
1. Push `123456789012.dkr.ecr.us-west-2.amazonaws.com/tools/ecs-sd:latest` to ECR

### Positional Parameters

* **`target_ecr_repository_url`**: The full URL of your target AWS ECR repository without tag.
* **`source_dockerhub_image_url` (optional)**: DockerHub image to pull (default: `apptality/aws-ecs-cloudmap-prometheus-discovery:latest`).
* **`target_ecr_repository_tag` (optional)**: ECR image tag (defaults to the source image tag).
* **`docker_image_platform` (optional)**: Image platform (`linux/amd64` by default).

> Note: you need to create destination ECR repository first.

## References

This application is heavily influenced by the following articles:

* [Metrics collection from Amazon ECS using Amazon Managed Service for Prometheus](https://aws.amazon.com/blogs/opensource/metrics-collection-from-amazon-ecs-using-amazon-managed-service-for-prometheus/)
* [Metrics and traces collection from Amazon ECS using AWS Distro for OpenTelemetry with dynamic service discovery](https://aws.amazon.com/blogs/containers/metrics-and-traces-collection-from-amazon-ecs-using-aws-distro-for-opentelemetry-with-dynamic-service-discovery/)

Definitely suggest reading up on what is [AWS Distro for OpenTelemetry (ADOT)](https://aws-otel.github.io/docs/introduction), as it provides open source APIs, libraries, and agents to collect logs, metrics, and traces from your applications.

For how to integrate AWS AMP (Prometheus) with your Grafana please refer to [Set up Grafana open source or Grafana Enterprise for use with Amazon Managed Service for Prometheus](https://docs.aws.amazon.com/prometheus/latest/userguide/AMP-onboard-query-standalone-grafana.html) article by AWS.

## Alternatives

There are definitely some discovery tools that are alternative to current application:

* [Container Insights Prometheus metrics monitoring](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/ContainerInsights-Prometheus-Setup-autodiscovery-ecs.html) discovers your infrastructure using CloudWatch
* [aws-cloudmap-prometheus-sd](https://github.com/awslabs/aws-cloudmap-prometheus-sd) plugin by AWS
* [prometheus-for-ecs](https://github.com/aws-samples/prometheus-for-ecs) plugin by AWS. Definitely dig into [deploy-prometheus](https://github.com/aws-samples/prometheus-for-ecs/tree/main/deploy-prometheus) and [deploy-adot](https://github.com/aws-samples/prometheus-for-ecs/tree/main/deploy-adot) folders of the repository to get a good understanding of how integrations are being set up.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues to discuss potential improvements or features.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
