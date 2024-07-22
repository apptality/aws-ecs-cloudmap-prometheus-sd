using Amazon.ServiceDiscovery.Model;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.CloudMap;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;
using Apptality.CloudMapEcsPrometheusDiscovery.Infrastructure;
using Apptality.CloudMapEcsPrometheusDiscovery.Models;
using Apptality.CloudMapEcsPrometheusDiscovery.Responses;

var app = WebApplication
    .CreateBuilder(args)
    // Add infrastructure services
    .AddInfrastructure()
    // Add discovery services
    .AddDiscovery()
    .Build();

// Use infrastructure services
app.UseInfrastructure();

// Log startup
var logger = app.Services.GetService<ILogger<Program>>()!;
logger.LogInformation("Started 'ECS Prometheus Discovery' application");

app.MapGet("/prometheus-ecs-targets", async (IEcsDiscovery ecsDiscovery) =>
{
    var ecsClusters = await ecsDiscovery.GetClusters();
    //return Results.Ok(ecsClusters);

    var firstCluster = ecsClusters.FirstOrDefault();
    if (firstCluster==null) { return Results.NoContent(); }

    var ecsClusterServices = await ecsDiscovery.GetClusterServices(firstCluster.ClusterArn);
    //return Results.Ok(ecsClusterServices);

    var ecsCloudMapServices = await ecsDiscovery.GetCloudMapServices(firstCluster.ServiceConnectDefaults?.Namespace ?? "");
    //return Results.Ok(ecsCloudMapServices);

    var ecsServicesDescriptions = await ecsDiscovery.DescribeServices(ecsCloudMapServices.ServiceArns);
    //return Results.Ok(ecsServicesDescriptions);

    var taskDefinitions = ecsServicesDescriptions.Select(s => s.TaskDefinition).ToList();
    //return Results.Ok(taskDefinitions);

    var firstServiceArn = ecsCloudMapServices.ServiceArns.FirstOrDefault();
    if (firstServiceArn == null) { return Results.NoContent(); }

    var runningTasks = await ecsDiscovery.GetRunningTasks(firstCluster.ClusterArn, firstServiceArn);
    //return Results.Ok(runningTasks);

    var runningTasksDescriptions = await ecsDiscovery.DescribeTasks(firstCluster.ClusterArn, runningTasks.RunningTaskArns);
    // return Results.Ok(runningTasksDescriptions);

    var runningTasksDefinition = await ecsDiscovery.DescribeTaskDefinition(taskDefinitions.First());
    return Results.Ok(runningTasksDefinition);
});

// Map endpoint
app.MapGet("/prometheus-cm-targets", async (ILogger<Program> l, IServiceDiscovery cmDiscoveryService) =>
{
    try
    {
        // List all namespaces
        var cmNamespaces = await cmDiscoveryService.GetNamespaces();
        var firstNamespaceTags = await cmDiscoveryService.GetTags(cmNamespaces.First().Arn);
        //return Results.Ok(firstNamespaceTags);

        // List all service within namespaces
        var cnNamespacesIds = cmNamespaces.Select(ns => ns.Id).ToList();
        var cmServices = await cmDiscoveryService.GetServices(cnNamespacesIds.ToArray());

        var firstService = cmServices?.FirstOrDefault(s=>s.Name == "service-http"); //metrics-sd-
        var firstServiceTags = await cmDiscoveryService.GetTags(firstService.Arn);
        //return Results.Ok(firstServiceTags);
        if (firstService == null) { return Results.NoContent(); }

        var serviceInstances = await cmDiscoveryService.GetServiceInstances(firstService.Id);
        // return Results.Ok(serviceInstances);
        var firstServiceInstance = serviceInstances?.FirstOrDefault()!;

        var serviceId = firstService.Id;
        var instanceId = firstServiceInstance.Id;
        var instance = await cmDiscoveryService.GetInstance(serviceId, instanceId);

        //return Results.Ok(instance);

        var templateResponse = new PrometheusTargetsResponse
        {
            new()
            {
                Targets = ["127.0.0.1"],
                Labels = new Dictionary<string, string>
                {
                    {"job", "example"},
                    {"region", "us-west-2"}
                }
            },
            new()
            {
                Targets = ["127.0.0.2"],
                Labels = new Dictionary<string, string>
                {
                    {"job", "example-2"},
                    {"region", "us-west-2"}
                }
            },
        };
        return Results.Ok(templateResponse);
    }
    catch (Exception ex)
    {
        l.LogError(ex, "Failed to generate Prometheus response");
        return Results.Json(new
        {
            message = ex.Message,
            innerExceptionMessage = ex.InnerException?.Message
        }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

// Run Kestrel
app.Run();