using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RuleService.Repositories;
using RuleService.Models;

namespace RuleService.Tests;

public sealed class RuleServiceIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"rule-api-{Guid.NewGuid():N}");

    [Fact]
    public async Task InvalidPromotionReturnsProblemDetails()
    {
        using var factory = CreateFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/rules/promotion", new TimeWindowPromotionRule
        {
            Name = " ",
            DiscountPercentage = 101,
            EffectiveFrom = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutUpdatesRuleAndDuplicateNameReturnsConflictProblem()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var first = await Create(client, "First");
        await Create(client, "Second");
        var updated = first with { Name = "Updated", DiscountPercentage = 25 };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/rules/{first.Id}", updated)).StatusCode);
        var conflict = await client.PutAsJsonAsync($"/rules/{first.Id}", updated with { Name = "SECOND" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutMissingRuleReturnsNotFoundProblem()
    {
        using var factory = CreateFactory();
        var response = await factory.CreateClient().PutAsJsonAsync($"/rules/{Guid.NewGuid()}", Promotion("Missing"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/missing", "GET", HttpStatusCode.NotFound)]
    [InlineData("/rules", "PATCH", HttpStatusCode.MethodNotAllowed)]
    [InlineData("/rules/not-a-guid", "GET", HttpStatusCode.BadRequest)]
    public async Task FrameworkAndInvalidIdFailuresReturnProblemDetails(
        string path,
        string method,
        HttpStatusCode status)
    {
        using var factory = CreateFactory();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteMissingRuleRemainsIdempotent()
    {
        using var factory = CreateFactory();
        var response = await factory.CreateClient().DeleteAsync($"/rules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePersistenceFailureReturnsProblemDetails()
    {
        using var factory = CreateFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IRuleRepository>();
            services.AddSingleton<IRuleRepository>(new FailingDeleteRepository());
        }));
        var response = await factory.CreateClient().DeleteAsync($"/rules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Disk unavailable", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RuleStorage:Path"] = Path.Combine(_directory, "rules.json"),
                    ["RuleStorage:SeedWhenMissing"] = "false"
                }));
        });

    private static async Task<TimeWindowPromotionRule> Create(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/rules/promotion", Promotion(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeWindowPromotionRule>())!;
    }

    private static TimeWindowPromotionRule Promotion(string name) => new()
    {
        Name = name,
        DiscountPercentage = 10,
        EffectiveFrom = DateTime.UtcNow
    };

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private sealed class FailingDeleteRepository : IRuleRepository
    {
        public Task<IReadOnlyList<PricingRule>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PricingRule>>([]);

        public Task<PricingRule?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PricingRule?>(null);

        public Task<RepositoryResult> AddAsync(
            PricingRule rule,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryResult(true));

        public Task<RepositoryResult> UpdateAsync(
            PricingRule rule,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryResult(true));

        public Task<RepositoryResult> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryResult(
                false,
                RepositoryFailure.Persistence,
                "Disk unavailable."));
    }
}
