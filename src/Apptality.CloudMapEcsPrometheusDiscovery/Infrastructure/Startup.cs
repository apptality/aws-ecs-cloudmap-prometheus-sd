using Apptality.CloudMapEcsPrometheusDiscovery.Prometheus;
using Serilog;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Infrastructure;

/// <summary>
/// Startup class for adding infrastructure configurations
/// </summary>
internal static class Startup
{
    /// <summary>
    /// Adds application configurations to the WebApplicationBuilder
    /// </summary>
    private static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder)
    {
        // Clear existing configurations sources to avoid unpredictable behavior
        builder.Configuration.Sources.Clear();

        // Ensure that settings are loaded from the current directory
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

        // Add appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Add environment variables (to allow easy configuration via Docker environment variables)
        // Read more: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#non-prefixed-environment-variables
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Adds caching to the WebApplicationBuilder
    /// </summary>
    private static WebApplicationBuilder AddCaching(this WebApplicationBuilder builder)
    {
        // Caching is used to reduce load on AWS tagging APIs and improve performance
        builder.Services.AddMemoryCache();
        return builder;
    }

    /// <summary>
    /// Adds health checks to the WebApplicationBuilder
    /// </summary>
    private static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        // Health checks are used to monitor the application's health.
        // This is useful for monitoring and alerting purposes.
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    /// Adds logging to the WebApplicationBuilder
    /// </summary>
    private static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        // Configure static logger to log to console
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        // Serilog is used for logging in the application
        builder.Host.UseSerilog((ctx, config) =>
            config
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
        );

        // This allows using ILogger<T> injectables throughout the application
        builder.Services.AddLogging(lb => lb.AddSerilog(dispose: true));

        return builder;
    }

    /// <summary>
    /// Adds kestrel configurations to the WebApplicationBuilder
    /// </summary>
    /// <remarks>
    /// <para>
    /// Application supports configuring Kestrel server options via one of the following methods:
    /// <code>1. export ASPNETCORE_URLS='http://*:9001' &amp;&amp; dotnet CloudMapEcsPrometheusDiscovery.dll</code>
    /// <code>2. dotnet CloudMapEcsPrometheusDiscovery.dll --urls 'http://*:9001'</code>
    /// </para>
    /// In both cases, the application will listen on port 9001 on all interfaces.
    /// Read more at <a href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#configure-endpoints">Microsoft Docs</a>
    /// </remarks>
    private static WebApplicationBuilder AddKestrelUrlsSupport(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseUrls();
        return builder;
    }

    /// <summary>
    /// Adds response JSON source generator serializers to the WebApplicationBuilder
    /// </summary>
    /// <remarks>
    /// You can read more <a href="https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0">here</a>
    /// </remarks>
    private static WebApplicationBuilder AddResponseSerializers(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, PrometheusResponseSerializerContext.Default);
        });
        return builder;
    }

    /// <summary>
    /// Adds ProblemDetails middleware to the application
    /// </summary>
    /// <remarks>
    /// You can read more <a href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errors?view=aspnetcore-8.0">here</a>
    /// </remarks>
    private static WebApplicationBuilder AddCentralizedErrorHandling(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        return builder;
    }

    /// <summary>
    /// Adds all infrastructure configurations to the application
    /// </summary>
    internal static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        return builder
            .AddKestrelUrlsSupport()
            .AddConfiguration()
            .AddLogging()
            .AddHealthChecks()
            .AddCaching()
            .AddResponseSerializers()
            .AddCentralizedErrorHandling();
    }

    /// <summary>
    /// Enables using infrastructure services
    /// </summary>
    internal static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
    {
        return app.UseHealthChecks("/health").UseSerilogRequestLogging();
    }

    /// <summary>
    /// Enables centralized error handling
    /// </summary>
    internal static IApplicationBuilder UseCentralizedErrorHandling(this IApplicationBuilder app)
    {
        // Use exception handler to handle all exceptions by default
        app.UseExceptionHandler(exceptionHandlerApp =>
            exceptionHandlerApp.Run(async context =>
                await Results.Problem(statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context)
            )
        );
        return app;
    }
}