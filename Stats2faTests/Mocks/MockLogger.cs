using Microsoft.Extensions.Logging;
using Moq;

namespace Stats2faTests.Mocks;

public class MockLogger {
    [Fact]
    public void Test_Logger_Messages() {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act
        LogMessage(logger: mockLogger.Object, "Test message");
        LogMessage(logger: mockLogger.Object, "Error message", logLevel: LogLevel.Error);

        // Assert
        mockLogger.Verify(x => x.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Test message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            times: Times.Once);

        mockLogger.Verify(x => x.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            times: Times.Once);
    }

    private void LogMessage(ILogger logger, string message, LogLevel logLevel = LogLevel.Information) {
        logger.Log(logLevel: logLevel,
            new EventId(0),
            state: message,
            null,
            (state, exception) => state.ToString());
    }
}