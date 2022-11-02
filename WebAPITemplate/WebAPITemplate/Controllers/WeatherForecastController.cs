using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

namespace WebAPITemplate.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "USA", "India", "Russia", "The Great Britain"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly Tracer tracer;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            TracerProvider provider)
        {
            _logger = logger;
            tracer = provider.GetTracer("TraceSource");
        }

        [HttpGet("GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using (var span = tracer.StartActiveSpan("Get"))
            {
                span.SetAttribute("Method", "Returning the list of forecasts to the UI....");
                span.AddEvent("The list of forecasts is being returned to the Swagger......");
            }

            _logger.LogInformation("Returning the weather forecasts....");
            _logger.LogWarning("Returning the list to the UI......");

            return Enumerable.Range(1, 6).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 60),
                Place = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
