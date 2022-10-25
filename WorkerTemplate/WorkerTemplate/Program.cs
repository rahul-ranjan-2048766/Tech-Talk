using WorkerTemplate;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

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







// Configure important OpenTelemetry settings and the console exporter
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

await host.RunAsync();