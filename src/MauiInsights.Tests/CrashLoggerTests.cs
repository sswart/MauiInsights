using FluentAssertions;
using MauiInsights.CrashHandling;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MauiInsights.Tests
{
    public class CrashLoggerTests
    {
        [Fact]
        public async Task Writes_To_DiskAsync()
        {
            // Arrange
            var path = $"{Guid.NewGuid()}";
            Directory.CreateDirectory(path);
            var message = "Cannot do this";
            var logger = new CrashLogger(path);

            try
            {
                // Act
                logger.LogToFileSystem(new InvalidOperationException(message));
                var logs = await logger.GetCrashLog().ToListAsync();

                // Assert
                logs.Should().HaveCount(1);
                var log = logs.First();
                log.Message.Should().Be(message);
                log.ExceptionDetailsInfoList.Should().HaveCount(1);
                log.ExceptionDetailsInfoList.First().TypeName.Should().Contain(nameof(InvalidOperationException));
            }
            finally
            {
                logger.ClearCrashLog();
            }
        }

        [Fact]
        public async Task Can_Clear_Logs()
        {
            // Arrange
            var path = $"{Guid.NewGuid()}";
            Directory.CreateDirectory(path);
            var message = "Cannot do this";
            var logger = new CrashLogger(path);
            logger.LogToFileSystem(new InvalidOperationException(message));

            // Act
            logger.ClearCrashLog();

            // Assert
            (await logger.GetCrashLog().ToListAsync()).Should().BeEmpty();
        }

        [Fact]
        public void Directory_Not_Exists()
        {
            var path = $"{Guid.NewGuid()}";

            var act = () => new CrashLogger(path);

            act.Should().Throw<DirectoryNotFoundException>();
        }
    }
}
