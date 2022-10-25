using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.Trace;
using WebAPITemplate;
using WebAPITemplate.Controllers;

namespace WebAPITemplate.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // Arrange
            var logger = new Mock<ILogger<WeatherForecastController>>();
            var provider = new Mock<TracerProvider>();

            // Act
            var controller = new WeatherForecastController(logger.Object, provider.Object);
            var result = controller.Get();            

            // Assert
            Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(result);
            Assert.Equal(6, result.Count());
        }
    }
}