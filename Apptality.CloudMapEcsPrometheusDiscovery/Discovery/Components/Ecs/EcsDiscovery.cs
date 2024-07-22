using Amazon.ECS;
using Amazon.ECS.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Extensions;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;
using Apptality.CloudMapEcsPrometheusDiscovery.Extensions;
using Microsoft.Extensions.Options;
using EcsTask = Amazon.ECS.Model.Task;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;

/// <summary>
/// Concrete implementation of the ECS service discovery interface
/// </summary>
public class EcsDiscovery(
    ILogger<EcsDiscovery> logger,
    IOptions<DiscoveryOptions> discoveryOptions,
    IAmazonECS ecsClient
) : IEcsDiscovery
{
    /// <inheritdoc cref="IEcsDiscovery.GetClusters"/>
    /// <remarks>
    /// Read more about the DescribeClusters API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeClustersRequest.html">here</a>
    /// </remarks>
    public async Task<List<Cluster>> GetClusters()
    {
        var ecsClusters = discoveryOptions.Value.EcsClusters
            .Split(';')
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (ecsClusters.Count == 0) return [];

        logger.LogDebug("Fetching ECS clusters: {@EcsClusters}", ecsClusters);

        var request = new DescribeClustersRequest {Clusters = ecsClusters};

        var response = await ecsClient.DescribeClustersAsync(request);

        // Report any failures
        if (response.Failures.Count > 0)
        {
            logger.LogWarning("Failures while fetching ECS clusters: {@Failures}", response.Failures);
        }

        return response.Clusters;

        // TODO: Remove
        // [
        // {
        //     "activeServicesCount": 7,
        //     "attachments": [],
        //     "attachmentsStatus": null,
        //     "capacityProviders": [],
        //     "clusterArn": "arn:aws:ecs:us-west-2:123456789012:cluster/ecs-cluster-name",
        //     "clusterName": "ecs-cluster-name",
        //     "configuration": null,
        //     "defaultCapacityProviderStrategy": [],
        //     "pendingTasksCount": 0,
        //     "registeredContainerInstancesCount": 0,
        //     "runningTasksCount": 8,
        //     "serviceConnectDefaults": {
        //         "namespace": "arn:aws:servicediscovery:us-west-2:123456789012:namespace/ns-ke2ewbfvynxeu2ub"
        //     },
        //     "settings": [],
        //     "statistics": [],
        //     "status": "ACTIVE",
        //     "tags": []
        // }
        // ]
    }

    /// <inheritdoc cref="IEcsDiscovery.GetClusterServices(string)"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListServicesRequest.html">here</a>
    /// </remarks>
    public async Task<EcsClusterServices> GetClusterServices(string clusterArn)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));

        var request = new ListServicesRequest {Cluster = clusterArn};
        logger.LogDebug("Fetching ECS services for cluster: {ClusterArn}", clusterArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListServicesAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var serviceArns = responses.SelectMany(r => r.ServiceArns).ToArray();
        logger.LogDebug("In ECS cluster {ClusterArn}, found {ServiceCount} services: {@ServiceArns}", clusterArn,
            serviceArns.Length, serviceArns);

        return new EcsClusterServices(clusterArn, serviceArns);

        // TODO: Remove
        // {
        //     "clusterArn": "arn:aws:ecs:us-west-2:123456789012:cluster/ecs-cluster-name",
        //     "serviceArns": [
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-jobs",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/aws-open-telemetry",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-http",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/connector-app",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/postgres-service",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-grpc",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-http-ui-host"
        //         ]
        // }
    }

    /// <inheritdoc cref="IEcsDiscovery.GetCloudMapServices(string)"/>
    /// <remarks>
    /// Read more about the ListServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListServicesByNamespaceRequest.html">here</a>
    /// </remarks>
    public async Task<EcsCloudMapServices> GetCloudMapServices(string namespaceArn)
    {
        if (string.IsNullOrWhiteSpace(namespaceArn))
            throw new ArgumentException("CloudMap ARN must be provided.", nameof(namespaceArn));

        var request = new ListServicesByNamespaceRequest {Namespace = namespaceArn};
        logger.LogDebug("Fetching ECS services for CloudMap Namespace: {Namespace}", namespaceArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListServicesByNamespaceAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var serviceArns = responses.SelectMany(r => r.ServiceArns).ToArray();
        logger.LogDebug("In CloudMap namespace {Namespace} found following services: {@ServiceArns}", namespaceArn,
            serviceArns);

        return new EcsCloudMapServices(namespaceArn, serviceArns);

        // TODO: Remove
        // {
        //     "cloudMapNamespaceArn": "arn:aws:servicediscovery:us-west-2:123456789012:namespace/ns-ke2ewbfvynxeu2ub",
        //     "serviceArns": [
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/connector-app",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-grpc",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-http",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-http-ui-host",
        //     "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/service-jobs"
        //         ]
        // }
    }

    /// <inheritdoc cref="IEcsDiscovery.DescribeServices"/>
    /// <remarks>
    /// Read more about the DescribeServices API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeServicesRequest.html">here</a>
    /// </remarks>
    public async Task<List<Service>> DescribeServices(string[] serviceArns)
    {
        // First, group service ARNs by ECS cluster name
        var serviceArnsByCluster = serviceArns
            .GroupBy(arn => arn.GetEcsClusterName())
            .ToDictionary(g => g.Key, g => g.ToArray());

        var services = new List<Service>();

        foreach (var (clusterName, clusterServiceArns) in serviceArnsByCluster)
        {
            // Divide clusterServiceArns into batches of 10
            var clusterServiceArnsBatches = clusterServiceArns
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / 10)
                .Select(g => g.Select(x => x.Value).ToList())
                .ToList();

            foreach (var ecsServiceArnsBatch in clusterServiceArnsBatches)
            {
                // This request supports describing up to 10 services at a time
                var request = new DescribeServicesRequest
                {
                    Cluster = clusterName,
                    Include = ["TAGS"],
                    Services = ecsServiceArnsBatch
                };

                logger.LogDebug(
                    "Fetching ECS services batch for cluster {ClusterName} with the following ECS Service ARNs: {@ServiceArns}",
                    clusterName, ecsServiceArnsBatch);

                var response = await ecsClient.DescribeServicesAsync(request);

                // Trim events, as these are non-relevant and just add noise
                response.Services.ForEach(s => s.Events = null);

                logger.LogDebug(
                    "Fetched ECS services batch for cluster {ClusterName} with the following response: {@Services}",
                    clusterName, response.Services);

                services.AddRange(response.Services);
            }
        }

        return services;

        // TODO: Remove
        // [{
        //   "capacityProviderStrategy": [],
        //   "clusterArn": "arn:aws:ecs:us-west-2:123456789012:cluster/ecs-cluster-name",
        //   "createdAt": "2024-07-13T05:23:12.3329999Z",
        //   "createdBy": "arn:aws:iam::123456789012:role/assume_role",
        //   "deploymentConfiguration": {
        //     "alarms": null,
        //     "deploymentCircuitBreaker": {
        //       "enable": false,
        //       "rollback": false
        //     },
        //     "maximumPercent": 200,
        //     "minimumHealthyPercent": 100
        //   },
        //   "deploymentController": {
        //     "type": {
        //       "value": "ECS"
        //     }
        //   },
        //   "deployments": [
        //     {
        //       "capacityProviderStrategy": [],
        //       "createdAt": "2024-07-17T18:38:59.904Z",
        //       "desiredCount": 1,
        //       "failedTasks": 0,
        //       "fargateEphemeralStorage": null,
        //       "id": "ecs-svc/1870513641342596493",
        //       "launchType": {
        //         "value": "FARGATE"
        //       },
        //       "networkConfiguration": {
        //         "awsvpcConfiguration": {
        //           "assignPublicIp": {
        //             "value": "DISABLED"
        //           },
        //           "securityGroups": [
        //             "sg-0b71246e22b143227"
        //           ],
        //           "subnets": [
        //             "subnet-0ceb5e9f0f1b6f84e",
        //             "subnet-0934a57400242b034"
        //           ]
        //         }
        //       },
        //       "pendingCount": 0,
        //       "platformFamily": "Linux",
        //       "platformVersion": "1.4.0",
        //       "rolloutState": {
        //         "value": "COMPLETED"
        //       },
        //       "rolloutStateReason": "ECS deployment ecs-svc/1870513641342596493 completed.",
        //       "runningCount": 1,
        //       "serviceConnectConfiguration": {
        //         "enabled": true,
        //         "logConfiguration": {
        //           "logDriver": {
        //             "value": "awslogs"
        //           },
        //           "options": {
        //             "awslogs-group": "/app/dev/application",
        //             "awslogs-region": "us-west-2",
        //             "awslogs-stream-prefix": "connector-app-service-connect"
        //           },
        //           "secretOptions": []
        //         },
        //         "namespace": "arn:aws:servicediscovery:us-west-2:123456789012:namespace/ns-ke2ewbfvynxeu2ub",
        //         "services": [
        //           {
        //             "clientAliases": [
        //               {
        //                 "dnsName": "connector-app",
        //                 "port": 8080
        //               }
        //             ],
        //             "discoveryName": "connector-app",
        //             "ingressPortOverride": 0,
        //             "portName": "application",
        //             "timeout": {
        //               "idleTimeoutSeconds": 0,
        //               "perRequestTimeoutSeconds": 0
        //             },
        //             "tls": null
        //           }
        //         ]
        //       },
        //       "serviceConnectResources": [
        //         {
        //           "discoveryArn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-wwyvagy22xd2znkg",
        //           "discoveryName": "connector-app"
        //         }
        //       ],
        //       "status": "PRIMARY",
        //       "taskDefinition": "arn:aws:ecs:us-west-2:123456789012:task-definition/ecs-connector-app:11",
        //       "updatedAt": "2024-07-20T01:23:05.2139999Z",
        //       "volumeConfigurations": []
        //     }
        //   ],
        //   "desiredCount": 1,
        //   "enableECSManagedTags": true,
        //   "enableExecuteCommand": false,
        //   "events": null,
        //   "healthCheckGracePeriodSeconds": 0,
        //   "launchType": {
        //     "value": "FARGATE"
        //   },
        //   "loadBalancers": [
        //     {
        //       "containerName": "connector-app",
        //       "containerPort": 8080,
        //       "loadBalancerName": null,
        //       "targetGroupArn": "arn:aws:elasticloadbalancing:us-west-2:123456789012:targetgroup/ecs-tg-conn-fdrs/a6695168294c7859"
        //     }
        //   ],
        //   "networkConfiguration": {
        //     "awsvpcConfiguration": {
        //       "assignPublicIp": {
        //         "value": "DISABLED"
        //       },
        //       "securityGroups": [
        //         "sg-0b71246e22b143227"
        //       ],
        //       "subnets": [
        //         "subnet-0ceb5e9f0f1b6f84e",
        //         "subnet-0934a57400242b034"
        //       ]
        //     }
        //   },
        //   "pendingCount": 0,
        //   "placementConstraints": [],
        //   "placementStrategy": [],
        //   "platformFamily": "Linux",
        //   "platformVersion": "LATEST",
        //   "propagateTags": {
        //     "value": "SERVICE"
        //   },
        //   "roleArn": "arn:aws:iam::123456789012:role/aws-service-role/ecs.amazonaws.com/AWSServiceRoleForECS",
        //   "runningCount": 1,
        //   "schedulingStrategy": {
        //     "value": "REPLICA"
        //   },
        //   "serviceArn": "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/connector-app",
        //   "serviceName": "connector-app",
        //   "serviceRegistries": [
        //     {
        //       "containerName": "connector-app",
        //       "containerPort": 0,
        //       "port": 0,
        //       "registryArn": "arn:aws:servicediscovery:us-west-2:123456789012:service/srv-xn5n4vq6waohzjfe"
        //     }
        //   ],
        //   "status": "ACTIVE",
        //   "tags": [
        //     {
        //       "key": "METRICS_PATH",
        //       "value": "/metrics"
        //     },
        //     {
        //       "key": "ECS_METRICS_PORT",
        //       "value": "9779"
        //     },
        //     {
        //       "key": "app_environment",
        //       "value": "dev"
        //     },
        //     {
        //       "key": "app_component",
        //       "value": "application"
        //     },
        //     {
        //       "key": "terraform",
        //       "value": "true"
        //     },
        //     {
        //       "key": "METRICS_PORT",
        //       "value": "8080"
        //     },
        //     {
        //       "key": "repository",
        //       "value": "terraform"
        //     },
        //     {
        //       "key": "ECS_METRICS_PATH",
        //       "value": "/metrics"
        //     }
        //   ],
        //   "taskDefinition": "arn:aws:ecs:us-west-2:123456789012:task-definition/ecs-connector-app:11",
        //   "taskSets": []
        //  },
        // ]
    }

    /// <inheritdoc cref="IEcsDiscovery.GetRunningTasks"/>
    /// <remarks>
    /// Read more about the ListTasks API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TListTasksRequest.html">here</a>
    /// </remarks>
    public async Task<EcsRunningTask> GetRunningTasks(string clusterArn, string serviceArn)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));
        if (string.IsNullOrWhiteSpace(serviceArn))
            throw new ArgumentException("Service ARN must be provided.", nameof(serviceArn));

        var request = new ListTasksRequest
        {
            Cluster = clusterArn,
            ServiceName = serviceArn,
            DesiredStatus = DesiredStatus.RUNNING
        };

        logger.LogDebug("Fetching running tasks for ECS service {ServiceArn} in cluster {ClusterArn}", serviceArn, clusterArn);

        var responses = await ecsClient.FetchAllPagesAsync(
            request,
            async req => await ecsClient.ListTasksAsync(req),
            resp => resp.NextToken,
            (requestContinuation, nextToken) => requestContinuation.NextToken = nextToken
        );

        var runningTaskArns = responses.SelectMany(response => response.TaskArns).ToArray();

        logger.LogDebug("In ECS cluster {ClusterArn}, found {RunningTaskCount} running tasks for service {ServiceArn}: {@RunningTaskArns}",
            clusterArn, runningTaskArns.Length, serviceArn, runningTaskArns);

        return new EcsRunningTask(clusterArn, serviceArn, runningTaskArns);

        // TODO: Remove
        // {
        //     "clusterArn": "arn:aws:ecs:us-west-2:123456789012:cluster/ecs-cluster-name",
        //     "serviceArn": "arn:aws:ecs:us-west-2:123456789012:service/ecs-cluster-name/connector-app",
        //     "runningTaskArns": [
        //     "arn:aws:ecs:us-west-2:123456789012:task/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5"
        //         ]
        // }
    }

    /// <inheritdoc cref="IEcsDiscovery.DescribeTasks"/>
    /// <remarks>
    /// Read more about the DescribeTasks API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeTasksRequest.html">here</a>
    /// </remarks>
    public async Task<List<EcsTask>> DescribeTasks(string clusterArn, string[] taskArns)
    {
        if (string.IsNullOrWhiteSpace(clusterArn))
            throw new ArgumentException("Cluster ARN must be provided.", nameof(clusterArn));

        if (taskArns.Length == 0) return [];

        // Divide taskArns into batches of 100, as this is the maximum number of tasks that can be described at once
        var taskArnsBatches = taskArns
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 100)
            .Select(g => g.Select(x => x.Value).ToList())
            .ToList();

        var tasks = new List<EcsTask>();

        // Fetch tasks in batches
        foreach (var taskArnsBatch in taskArnsBatches)
        {
            var request = new DescribeTasksRequest {Cluster = clusterArn, Tasks = taskArnsBatch};

            var response = await ecsClient.DescribeTasksAsync(request);
            tasks.AddRange(response.Tasks);
        }

        return tasks;

        // TODO: Remove
        // [
        //   {
        //     "attachments": [
        //       {
        //         "details": [
        //           {
        //             "name": "ContainerName",
        //             "value": "ecs-service-connect-oRK9o"
        //           }
        //         ],
        //         "id": "4fe35242-84f7-4e36-a118-97e1bb82433c",
        //         "status": "ATTACHED",
        //         "type": "ServiceConnect"
        //       },
        //       {
        //         "details": [
        //           {
        //             "name": "subnetId",
        //             "value": "subnet-0ceb5e9f0f1b6f84e"
        //           },
        //           {
        //             "name": "networkInterfaceId",
        //             "value": "eni-0ddc4544c2bf8af6e"
        //           },
        //           {
        //             "name": "macAddress",
        //             "value": "06:8b:26:17:78:f9"
        //           },
        //           {
        //             "name": "privateDnsName",
        //             "value": "ip-10-200-65-120.us-west-2.compute.internal"
        //           },
        //           {
        //             "name": "privateIPv4Address",
        //             "value": "10.200.65.120"
        //           }
        //         ],
        //         "id": "acd52fd1-f811-4686-862d-6101959acf95",
        //         "status": "ATTACHED",
        //         "type": "ElasticNetworkInterface"
        //       }
        //     ],
        //     "attributes": [
        //       {
        //         "name": "ecs.cpu-architecture",
        //         "targetId": null,
        //         "targetType": null,
        //         "value": "x86_64"
        //       }
        //     ],
        //     "availabilityZone": "us-west-2a",
        //     "capacityProviderName": null,
        //     "clusterArn": "arn:aws:ecs:us-west-2:123456789012:cluster/ecs-cluster-name",
        //     "connectivity": {
        //       "value": "CONNECTED"
        //     },
        //     "connectivityAt": "2024-07-17T18:39:20.25Z",
        //     "containerInstanceArn": null,
        //     "containers": [
        //       {
        //         "containerArn": "arn:aws:ecs:us-west-2:123456789012:container/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5/1b5d3e4e-119e-49c4-bf89-400f1d74819b",
        //         "cpu": null,
        //         "exitCode": 0,
        //         "gpuIds": [],
        //         "healthStatus": {
        //           "value": "HEALTHY"
        //         },
        //         "image": null,
        //         "imageDigest": null,
        //         "lastStatus": "RUNNING",
        //         "managedAgents": [],
        //         "memory": null,
        //         "memoryReservation": null,
        //         "name": "ecs-service-connect-oRK9o",
        //         "networkBindings": [],
        //         "networkInterfaces": [
        //           {
        //             "attachmentId": "acd52fd1-f811-4686-862d-6101959acf95",
        //             "ipv6Address": null,
        //             "privateIpv4Address": "10.200.65.120"
        //           }
        //         ],
        //         "reason": null,
        //         "runtimeId": null,
        //         "taskArn": "arn:aws:ecs:us-west-2:123456789012:task/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5"
        //       },
        //       {
        //         "containerArn": "arn:aws:ecs:us-west-2:123456789012:container/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5/77cb801b-e8e4-4ab1-a532-a8b33a88fdec",
        //         "cpu": "0",
        //         "exitCode": 0,
        //         "gpuIds": [],
        //         "healthStatus": {
        //           "value": "UNKNOWN"
        //         },
        //         "image": "public.ecr.aws/awsvijisarathy/ecs-exporter:1.2",
        //         "imageDigest": "sha256:7e0c0601c34c2e704b0a6a90546b4aad8c7dc226ceb5b1b065d5895a062b0abd",
        //         "lastStatus": "RUNNING",
        //         "managedAgents": [],
        //         "memory": null,
        //         "memoryReservation": null,
        //         "name": "ecs-exporter",
        //         "networkBindings": [],
        //         "networkInterfaces": [
        //           {
        //             "attachmentId": "acd52fd1-f811-4686-862d-6101959acf95",
        //             "ipv6Address": null,
        //             "privateIpv4Address": "10.200.65.120"
        //           }
        //         ],
        //         "reason": null,
        //         "runtimeId": "5ad776f381d947ffb6f6d60d77912ce5-4159844948",
        //         "taskArn": "arn:aws:ecs:us-west-2:123456789012:task/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5"
        //       },
        //       {
        //         "containerArn": "arn:aws:ecs:us-west-2:123456789012:container/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5/95309c95-169d-4fab-b17f-9af6bbef3b88",
        //         "cpu": "0",
        //         "exitCode": 0,
        //         "gpuIds": [],
        //         "healthStatus": {
        //           "value": "HEALTHY"
        //         },
        //         "image": "481928005500.dkr.ecr.us-west-2.amazonaws.com/application/connector-app:aws-dev",
        //         "imageDigest": "sha256:3fb2eac06456b2a7e4cfb198b129db571a6a653a3f7bc0c65fd9932e96bf9590",
        //         "lastStatus": "RUNNING",
        //         "managedAgents": [],
        //         "memory": null,
        //         "memoryReservation": null,
        //         "name": "connector-app",
        //         "networkBindings": [],
        //         "networkInterfaces": [
        //           {
        //             "attachmentId": "acd52fd1-f811-4686-862d-6101959acf95",
        //             "ipv6Address": null,
        //             "privateIpv4Address": "10.200.65.120"
        //           }
        //         ],
        //         "reason": null,
        //         "runtimeId": "5ad776f381d947ffb6f6d60d77912ce5-3127987929",
        //         "taskArn": "arn:aws:ecs:us-west-2:123456789012:task/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5"
        //       }
        //     ],
        //     "cpu": "512",
        //     "createdAt": "2024-07-17T18:39:16.4059998Z",
        //     "desiredStatus": "RUNNING",
        //     "enableExecuteCommand": false,
        //     "ephemeralStorage": {
        //       "sizeInGiB": 20
        //     },
        //     "executionStoppedAt": "0001-01-01T00:00:00",
        //     "fargateEphemeralStorage": {
        //       "kmsKeyId": null,
        //       "sizeInGiB": 20
        //     },
        //     "group": "service:connector-app",
        //     "healthStatus": {
        //       "value": "HEALTHY"
        //     },
        //     "inferenceAccelerators": [],
        //     "lastStatus": "RUNNING",
        //     "launchType": {
        //       "value": "FARGATE"
        //     },
        //     "memory": "1024",
        //     "overrides": {
        //       "containerOverrides": [
        //         {
        //           "command": [],
        //           "cpu": 0,
        //           "environment": [
        //             {
        //               "name": "ENVOY_CONCURRENCY",
        //               "value": "2"
        //             },
        //             {
        //               "name": "ENVOY_ADMIN_MODE",
        //               "value": "UDS"
        //             },
        //             {
        //               "name": "ENABLE_STATS_SNAPSHOT",
        //               "value": "true"
        //             },
        //             {
        //               "name": "APPMESH_METRIC_EXTENSION_VERSION",
        //               "value": "1"
        //             },
        //             {
        //               "name": "APPNET_ENVOY_RESTART_COUNT",
        //               "value": "3"
        //             },
        //             {
        //               "name": "HC_DISCONNECTED_TIMEOUT_S",
        //               "value": "10800"
        //             },
        //             {
        //               "name": "APPNET_AGENT_ADMIN_MODE",
        //               "value": "UDS"
        //             },
        //             {
        //               "name": "APPMESH_RESOURCE_ARN",
        //               "value": "arn:aws:ecs:us-west-2:123456789012:task-set/ecs-cluster-name/connector-app/ecs-svc/1870513641342596493"
        //             }
        //           ],
        //           "environmentFiles": [],
        //           "memory": 0,
        //           "memoryReservation": 0,
        //           "name": "ecs-service-connect-oRK9o",
        //           "resourceRequirements": []
        //         },
        //         {
        //           "command": [],
        //           "cpu": 0,
        //           "environment": [],
        //           "environmentFiles": [],
        //           "memory": 0,
        //           "memoryReservation": 0,
        //           "name": "ecs-exporter",
        //           "resourceRequirements": []
        //         },
        //         {
        //           "command": [],
        //           "cpu": 0,
        //           "environment": [],
        //           "environmentFiles": [],
        //           "memory": 0,
        //           "memoryReservation": 0,
        //           "name": "connector-app",
        //           "resourceRequirements": []
        //         }
        //       ],
        //       "cpu": null,
        //       "ephemeralStorage": null,
        //       "executionRoleArn": null,
        //       "inferenceAcceleratorOverrides": [],
        //       "memory": null,
        //       "taskRoleArn": null
        //     },
        //     "platformFamily": "Linux",
        //     "platformVersion": "1.4.0",
        //     "pullStartedAt": "2024-07-17T18:39:26.6549999Z",
        //     "pullStoppedAt": "2024-07-17T18:39:34.9649999Z",
        //     "startedAt": "2024-07-17T18:40:26.7039999Z",
        //     "startedBy": "ecs-svc/1870513641342596493",
        //     "stopCode": null,
        //     "stoppedAt": "0001-01-01T00:00:00",
        //     "stoppedReason": null,
        //     "stoppingAt": "0001-01-01T00:00:00",
        //     "tags": [],
        //     "taskArn": "arn:aws:ecs:us-west-2:123456789012:task/ecs-cluster-name/5ad776f381d947ffb6f6d60d77912ce5",
        //     "taskDefinitionArn": "arn:aws:ecs:us-west-2:123456789012:task-definition/ecs-connector-app:11",
        //     "version": 5
        //   }
        // ]
    }

    /// <inheritdoc cref="IEcsDiscovery.DescribeTaskDefinition"/>
    /// <remarks>
    /// Read more about the DescribeTaskDefinition API operation <a href="https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ECS/TDescribeTaskDefinitionRequest.html">here</a>
    /// </remarks>
    public async Task<TaskDefinition> DescribeTaskDefinition(string taskDefinitionArn)
    {
        if(string.IsNullOrWhiteSpace(taskDefinitionArn))
            throw new ArgumentException("Task definition ARN must be provided.", nameof(taskDefinitionArn));

        var request = new DescribeTaskDefinitionRequest
        {
            Include = ["TAGS"],
            TaskDefinition = taskDefinitionArn
        };

        var response = await ecsClient.ExecuteWithRetryAsync(async () => await ecsClient.DescribeTaskDefinitionAsync(request));

        return response.TaskDefinition;
    }
}