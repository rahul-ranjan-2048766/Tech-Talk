using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Adding OpenTelemetry using ELK
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

// Add OpenTelemetry
builder.Services.AddOpenTelemetryTracing(x =>
{
    x.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("WebAPITemplate"));

    x.AddSource("TraceSource");
    
    x.AddAspNetCoreInstrumentation(sam => sam.Filter = httpContext => 
        !httpContext.Request.Path.Value?
        .Contains("/_framework/aspnetcore-browser-refresh.js") ?? true);
    
    x.AddHttpClientInstrumentation(sam => sam.Enrich = 
    (activity, eventName, rawObject) =>
    {
        if (eventName == "OnStartActivity" 
        && rawObject is HttpRequestMessage request
        && request.Method == HttpMethod.Get)
            activity.SetTag("WebAPITemplate", "This is a microservice.");
    });
    
    x.AddEntityFrameworkCoreInstrumentation(sam =>
    {
        sam.SetDbStatementForStoredProcedure = true;
        sam.SetDbStatementForText = true;
    });
    
    x.AddConsoleExporter();
    
    x.AddJaegerExporter(sam =>
    {
        sam.AgentHost = "localhost";
        sam.AgentPort = 6831;
    });
    
    x.AddZipkinExporter(sam =>
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

app.MapMetrics();

app.UseHttpMetrics();

app.Run();

