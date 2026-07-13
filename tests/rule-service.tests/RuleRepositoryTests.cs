using Microsoft.Extensions.Logging.Abstractions;
using RuleService.Models;
using RuleService.Repositories;

namespace RuleService.Tests;

public sealed class RuleRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"rules-{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_directory, "rules.json");

    [Fact]
    public async Task UsesInjectedPathAndReturnsSnapshot()
    {
        using var repo = CreateRepository();
        var rule = Promotion("Snapshot");
        Assert.True((await repo.AddAsync(rule)).Success);

        var snapshot = await repo.GetAllAsync();
        await repo.DeleteAsync(rule.Id);

        Assert.Single(snapshot);
        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public async Task ConcurrentAddsAllowOnlyOneCaseInsensitiveName()
    {
        using var repo = CreateRepository();
        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(i => repo.AddAsync(Promotion(i % 2 == 0 ? "Express" : "EXPRESS"))));

        Assert.Single(results, result => result.Success);
        Assert.Single(await repo.GetAllAsync());
    }

    [Fact]
    public async Task UpdateReportsNotFoundAndDuplicateConflict()
    {
        using var repo = CreateRepository();
        var first = Promotion("First");
        var second = Promotion("Second");
        await repo.AddAsync(first);
        await repo.AddAsync(second);

        var missing = await repo.UpdateAsync(Promotion("Missing") with { Id = Guid.NewGuid() });
        var duplicate = await repo.UpdateAsync(second with { Name = "FIRST" });

        Assert.Equal(RepositoryFailure.NotFound, missing.Failure);
        Assert.Equal(RepositoryFailure.Conflict, duplicate.Failure);
    }

    [Fact]
    public async Task AddRejectsDuplicateId()
    {
        using var repo = CreateRepository();
        var first = Promotion("First");
        await repo.AddAsync(first);
        var result = await repo.AddAsync(Promotion("Second") with { Id = first.Id });
        Assert.Equal(RepositoryFailure.Conflict, result.Failure);
    }

    [Fact]
    public async Task PersistedRulesCanBeReopened()
    {
        var rule = Promotion("Durable");
        using (var writer = CreateRepository()) Assert.True((await writer.AddAsync(rule)).Success);
        using var reader = CreateRepository();
        Assert.Equal(rule, await reader.GetByIdAsync(rule.Id));
    }

    [Fact]
    public async Task RepositoryInstancesCoordinateMutationsForSamePath()
    {
        using var first = CreateRepository();
        using var second = CreateRepository();
        await Task.WhenAll(first.AddAsync(Promotion("One")), second.AddAsync(Promotion("Two")));
        using var reopened = CreateRepository();
        Assert.Equal(2, (await reopened.GetAllAsync()).Count);
    }

    [Fact]
    public async Task ExistingReaderRefreshesAfterAnotherInstanceWrites()
    {
        using var reader = CreateRepository();
        using var writer = CreateRepository();
        var rule = Promotion("Fresh");
        await writer.AddAsync(rule);
        Assert.Equal(rule, await reader.GetByIdAsync(rule.Id));
        Assert.Single(await reader.GetAllAsync());
    }

    [Fact]
    public async Task MutationRecoversFromCorruptFile()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(FilePath, "{ corrupt");
        using var repo = CreateRepository();
        var result = await repo.AddAsync(Promotion("Recovered"));
        Assert.True(result.Success);
        using var reopened = CreateRepository();
        Assert.Single(await reopened.GetAllAsync());
    }

    private JsonRuleRepository CreateRepository() =>
        new(FilePath, NullLogger<JsonRuleRepository>.Instance, seedWhenMissing: false);

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
}
