using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using PricingService.Models;
using PricingService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using Xunit;

namespace PricingService.Tests;

public class PricingServiceIntegrationTests : IClassFixture<PricingServiceFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PricingServiceIntegrationTests(PricingServiceFactory factory)
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
        var result = await response.Content.ReadFromJsonAsync<BulkAcceptedResponse>();
        Assert.NotNull(result?.JobId);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["job_id"], json.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task JobResponse_DoesNotExposeSubmittedQuotes()
    {
        const string submittedArea = "private-customer-area";
        var client = _factory.CreateClient();
        var accepted = await client.PostAsJsonAsync(
            "/quotes/bulk",
            new BulkJobRequest
            {
                Quotes = [new QuoteRequest { Weight = 10, Area = submittedArea }]
            });
        var acceptedJob = await accepted.Content.ReadFromJsonAsync<BulkAcceptedResponse>();

        var response = await client.GetAsync($"/jobs/{acceptedJob!.JobId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain("request", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(submittedArea, responseBody, StringComparison.Ordinal);
    }


    [Fact]
    public async Task InvalidQuote_ReturnsProblemJson()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/quotes/price",
            new QuoteRequest { Weight = 0, Area = " " });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task InvalidCsvValue_ReturnsRowIndexedProblem()
    {
        var content = new MultipartFormDataContent();
        const string sensitiveMarker = "private-customer-value";
        content.Add(
            new StringContent($"Weight,Area\n10,City\n{sensitiveMarker},Remote"),
            "file",
            "bad.csv");

        var response = await _factory.CreateClient().PostAsync("/quotes/bulk", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("row 3", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sensitiveMarker, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidRuleServiceJson_ReturnsProblemJson()
    {
        await using var factory = PricingServiceFactory.CreateInvalidJson();
        var response = await factory.CreateClient().PostAsJsonAsync("/quotes/price",
            new QuoteRequest { Weight = 1, Area = "City" });

        Assert.Equal(System.Net.HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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

        var result = await response.Content.ReadFromJsonAsync<BulkAcceptedResponse>();
        Assert.NotNull(result?.JobId);
    }
}

public sealed class PricingServiceFactory : WebApplicationFactory<Program>
{
    private readonly bool _invalidJson;
    public PricingServiceFactory() { }
    private PricingServiceFactory(bool invalidJson) => _invalidJson = invalidJson;
    public static PricingServiceFactory CreateInvalidJson() => new(true);
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.RemoveAll<IRuleServiceClient>();
        services.AddSingleton<IRuleServiceClient>(new FakeRuleServiceClient(_invalidJson));
    });
}

public sealed class FakeRuleServiceClient(bool invalidJson = false) : IRuleServiceClient
{
    public Task<IReadOnlyList<PricingRuleDto>> GetRulesAsync(CancellationToken cancellationToken) => invalidJson
        ? Task.FromException<IReadOnlyList<PricingRuleDto>>(new JsonException("bad upstream payload"))
        : Task.FromResult<IReadOnlyList<PricingRuleDto>>([]);
}
