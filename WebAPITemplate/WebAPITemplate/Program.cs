using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

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
        $"{environment?.ToLower().Replace(".","-")}-{DateTime.UtcNow:yyyy-MM}"
    };
}

builder.Host.UseSerilog();

// Add OpenTelemetry (Jaeger + Zipkin)
builder.Services.AddOpenTelemetryTracing(x =>
{
    x.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("WebAPITemplate"));                    // creates resources: key-value pairs which describe your service

    x.AddSource("TraceSource");
    
    x.AddAspNetCoreInstrumentation(sam => sam.Filter = httpContext =>  // AspNetCore instrumentation library tracks the inbound
        !httpContext.Request.Path.Value?
        .Contains("/_framework/aspnetcore-browser-refresh.js") ?? true); // ASP.NET Core request and sends traces to a collector.

    x.AddHttpClientInstrumentation(sam => sam.Enrich =   // collects metrics and traces about outgoing HTTP requests.
    (activity, eventName, rawObject) =>
    {
        if (eventName == "OnStartActivity" 
        && rawObject is HttpRequestMessage request
        && request.Method == HttpMethod.Get)
            activity.SetTag("WebAPITemplate", "This is a microservice.");
    });
    
    x.AddEntityFrameworkCoreInstrumentation(sam =>  // collects metrics and traces about requests that ask 
    {
        sam.SetDbStatementForStoredProcedure = true;  // for database statements and procedures to be executed
        sam.SetDbStatementForText = true;
    });
    
    x.AddConsoleExporter();  // exports metrics, logs and traces to console
    
    x.AddJaegerExporter(sam =>  // exports metrics, logs and traces to Jaeger
    {
        sam.AgentHost = "localhost";
        sam.AgentPort = 6831;
    });
    
    x.AddZipkinExporter(sam =>  // exports metrics, logs and traces to Zipkin
    {
        sam.Endpoint = new Uri($"http://localhost:9411/api/v2/spans");
    });
});


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapMetrics();  // For Prometheus

app.UseHttpMetrics();  // For Prometheus

app.Run();

