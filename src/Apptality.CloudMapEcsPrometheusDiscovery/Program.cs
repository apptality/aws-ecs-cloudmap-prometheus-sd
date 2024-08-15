using Apptality.CloudMapEcsPrometheusDiscovery.Discovery;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Factories;
using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Services;
using Apptality.CloudMapEcsPrometheusDiscovery.Infrastructure;
using Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;

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

// Define Prometheus targets endpoint
app.MapGet("/prometheus-targets", async (
    IDiscoveryService discoveryService,
    IDiscoveryTargetFactory discoveryTargetFactory
) =>
{
    // Discover infrastructure
    var discoveryResult = await discoveryService.Discover();

    // Convert to Discovery targets hierarchy
    var discoveryTargets = discoveryTargetFactory.Create(discoveryResult);

    // Create Prometheus response
    var response = PrometheusResponseFactory.Create(discoveryTargets);

    return Results.Ok(response);
});

// Run Kestrel
app.Run();