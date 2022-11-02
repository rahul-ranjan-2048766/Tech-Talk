using WorkerTemplate;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Prometheus;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;


// Adding Prometheus - Metrics Calculation
var prom_ok = Metrics.CreateCounter("prom_ok", "Success Count");
var prom_warning = Metrics.CreateCounter("prom_warning", "Warning Count");
var prom_exception = Metrics.CreateCounter("prom_exception", "Exception Count");
prom_ok.Inc(1);
prom_warning.Inc(1);
prom_exception.Inc(1);

var metricServer = new MetricServer(1234);
metricServer.Start();
////// Check http://localhost:1234/metrics for metrics //////////////





string serviceName = "";
string serviceVersion = "";
string JaegerHost = "";
string JaegerPort = "";
string ZipkinHost = "";
string ZipkinPort = "";

// Adding OpenTelemetry - Exporting data to ELK
ConfigureLogs();

void ConfigureLogs()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Debug()
        .WriteTo.Console()
        .WriteTo.Elasticsearch(ConfigureELS(configuration, environment))
        .CreateLogger();
}

ElasticsearchSinkOptions ConfigureELS(IConfigurationRoot configuration, string? environment)
{
    return new ElasticsearchSinkOptions(new Uri($"http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name?.ToLower()}-" +
        $"{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
    };
}

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    { 
        services.AddHostedService<Worker>();
        serviceName = hostContext.Configuration["ServiceName"];
        serviceVersion = hostContext.Configuration["ServiceVersion"];
        JaegerHost = hostContext.Configuration["JaegerHost"];
        JaegerPort = hostContext.Configuration["JaegerPort"];
        ZipkinHost = hostContext.Configuration["ZipkinHost"];
        ZipkinPort = hostContext.Configuration["ZipkinPort"];
    })
    
    .UseSerilog()
    
    .Build();







// Adding Jaeger and Zipkin 
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)

    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))

    .AddConsoleExporter()

    .AddEntityFrameworkCoreInstrumentation(x =>
    {
        x.SetDbStatementForStoredProcedure = true;
        x.SetDbStatementForText = true;
    })

    .AddJaegerExporter(x =>
    {
        x.AgentHost = JaegerHost;
        x.AgentPort = int.Parse(JaegerPort);
    })

    .AddZipkinExporter(x =>
    {
        x.Endpoint = new Uri($"http://{ZipkinHost}:{ZipkinPort}/api/v2/spans");
    })
    .Build();
////////////////////////////

await host.RunAsync();