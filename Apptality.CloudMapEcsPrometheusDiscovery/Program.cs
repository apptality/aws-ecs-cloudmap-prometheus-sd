using Apptality.CloudMapEcsPrometheusDiscovery.Discovery;
using Apptality.CloudMapEcsPrometheusDiscovery.Infrastructure;

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
logger.LogInformation("Started 'ECS CloudMap Prometheus Discovery' application");

// Map endpoint
app.MapGet("/prometheus-targets", (ILogger<Program> l) =>
{
    l.LogInformation("Hello World!");
    return "Hello World!";
});

// Run Kestrel
app.Run();