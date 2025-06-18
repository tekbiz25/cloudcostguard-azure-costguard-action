using Xunit;

namespace AzCostguard.Tests;

public class AceWrapperTests
{
    [Fact]
    public async Task EstimateAsync_WithSampleBicep_ReturnsResourcesGreaterThanZero()
    {
        // Arrange
        var testDataPath = Path.Combine(Environment.CurrentDirectory, "TestData", "sample.bicep");
        var files = new[] { testDataPath };
        
        // Ensure test file exists
        Assert.True(File.Exists(testDataPath), $"Test file not found at: {testDataPath}");
        
        // Act
        var results = await AceWrapper.EstimateAsync(files);
        
        // Assert
        Assert.NotNull(results);
        Assert.True(results.Count > 0, "Expected at least one resource to be estimated");
        
        // Additional assertions for the structure
        var firstResult = results.First();
        Assert.NotEmpty(firstResult.ResourceId);
        Assert.NotEmpty(firstResult.Service);
        Assert.NotEmpty(firstResult.Sku);
        Assert.True(firstResult.EurosPerMonth >= 0, "Cost should be non-negative");
    }
    
    [Fact]
    public async Task EstimateAsync_WithEmptyFileList_ReturnsEmptyList()
    {
        // Arrange
        var emptyFiles = new string[0];
        
        // Act
        var results = await AceWrapper.EstimateAsync(emptyFiles);
        
        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }
} 