using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stats2faTests.Mocks;

// Mock of the StatsContext for testing purposes
public class MockStatsContext 
{
    [Fact]
    public void Test_Context_Initialization()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "Stats2faTests");
        
        // Act - Create folder if needed
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        
        // Assert
        Assert.True(Directory.Exists(tempPath));
    }
    
    [Fact]
    public void Test_Json_Serialization()
    {
        // Arrange
        var clientInfo = new ClientInformation
        {
            Id = "client123",
            Name = "Test Client"
        };
        
        // Act
        var json = JsonSerializer.Serialize(clientInfo);
        var deserialized = JsonSerializer.Deserialize<ClientInformation>(json);
        
        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("client123", deserialized.Id);
        Assert.Equal("Test Client", deserialized.Name);
    }
    
    // Mock classes to simulate database models
    public class ClientInformation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}