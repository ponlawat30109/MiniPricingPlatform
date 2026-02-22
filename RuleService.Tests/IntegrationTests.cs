using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using RuleService.Models;
using Xunit;

namespace RuleService.Tests;

public class RuleServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RuleServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllRules_ReturnsOkAndInitialData()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/rules");

        // Assert
        response.EnsureSuccessStatusCode();
        var rules = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(rules);
        Assert.NotEmpty(rules);
    }
}
