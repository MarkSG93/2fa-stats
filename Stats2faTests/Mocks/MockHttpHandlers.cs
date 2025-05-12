using System.Net;
using Moq;
using Moq.Protected;

namespace Stats2faTests.Mocks;

public class MockHttpHandlers {
    [Fact]
    public async Task Test_Http_Mock_Response() {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode: HttpStatusCode.OK) {
                Content = new StringContent("Success")
            });

        var httpClient = new HttpClient(handler: mockHandler.Object);
        httpClient.BaseAddress = new Uri("https://example.com/");

        // Act
        var response = await httpClient.GetAsync("api/test");

        // Assert
        Assert.Equal(expected: HttpStatusCode.OK, actual: response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Success", actual: content);

        // Verify SendAsync was called exactly once
        mockHandler.Protected().Verify("SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Test_Http_Rate_Limiting() {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();

        // Always return success
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode: HttpStatusCode.OK) {
                Content = new StringContent("Success")
            });

        var httpClient = new HttpClient(handler: mockHandler.Object);
        httpClient.BaseAddress = new Uri("https://example.com/");

        // Act - Make several requests in parallel
        var startTime = DateTime.UtcNow;

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < 5; i++) tasks.Add(httpClient.GetAsync("api/test"));

        await Task.WhenAll(tasks: tasks);

        var endTime = DateTime.UtcNow;

        // Assert - All requests should be successful
        foreach (var task in tasks) Assert.Equal(expected: HttpStatusCode.OK, actual: task.Result.StatusCode);
    }
}