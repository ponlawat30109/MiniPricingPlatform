using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using PricingService.Models;
using Xunit;

namespace PricingService.Tests;

public class PricingServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PricingServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Organize
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task SubmitBulkJob_ReturnsAccepted()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new BulkJobRequest
        {
            Quotes = new List<QuoteRequest>
            {
                new() { Weight = 10, Area = "Test" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/quotes/bulk", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(result?.JobId);
    }

    [Fact]
    public async Task SubmitBulkCsvJob_ReturnsAccepted()
    {
        // Arrange
        var client = _factory.CreateClient();
        var csvContent = "Weight,Area\n10,TestCity\n20,TestRemote";
        var content = new MultipartFormDataContent();
        var streamContent = new StringContent(csvContent);
        content.Add(streamContent, "file", "test.csv");

        // Act
        var response = await client.PostAsync("/quotes/bulk", content);

        // Assert
        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"CSV Upload failed: {response.StatusCode} - {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(result?.JobId);
    }
}
