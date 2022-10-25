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
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
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

            return Enumerable.Range(1, 6).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
