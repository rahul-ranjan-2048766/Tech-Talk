using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace WorkerTemplate.Tests
{
    public class WorkerTests
    {
        [Fact]
        public void Test1()
        {
            // Arrange
            var logger = new Mock<ILogger<Worker>>();
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["ServiceName"]).Returns("WorkerTemplate.Tests");
            config.Setup(x => x["ServiceVersion"]).Returns("1.0.0");

            var worker = new Worker(logger.Object, config.Object);

            // Act

            // Assert
        }
    }
}