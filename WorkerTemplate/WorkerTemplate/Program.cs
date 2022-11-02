using WorkerTemplate;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Prometheus;
using OpenTelemetry.Metrics;


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