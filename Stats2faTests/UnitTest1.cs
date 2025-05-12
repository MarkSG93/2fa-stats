using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stats2faTests;

public class BasicModelTests {
    [Fact]
    public void SimpleJsonTest() {
        // Arrange
        var json = @"{""id"": ""123"", ""name"": ""Test Name""}";

        // Act
        var result = JsonSerializer.Deserialize<TestModel>(json: json);

        // Assert
        Assert.NotNull(@object: result);
        Assert.Equal("123", actual: result.Id);
        Assert.Equal("Test Name", actual: result.Name);
    }

    private class TestModel {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}