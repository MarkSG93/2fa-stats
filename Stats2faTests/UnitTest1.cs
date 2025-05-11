namespace Stats2faTests;

public class BasicModelTests
{
    [Fact]
    public void SimpleJsonTest()
    {
        // Arrange
        string json = @"{""id"": ""123"", ""name"": ""Test Name""}";

        // Act
        var result = System.Text.Json.JsonSerializer.Deserialize<TestModel>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result.Id);
        Assert.Equal("Test Name", result.Name);
    }

    private class TestModel
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
