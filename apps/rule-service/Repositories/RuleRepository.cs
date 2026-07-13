using System.Text.Json;
using System.Text.Json.Serialization;
using RuleService.Models;
using System.Collections.Concurrent;

namespace RuleService.Repositories;

public enum RepositoryFailure { None, Conflict, NotFound, Persistence }
public readonly record struct RepositoryResult(
    bool Success,
    RepositoryFailure Failure = RepositoryFailure.None,
    string? Error = null);

public interface IRuleRepository
{
    Task<IReadOnlyList<PricingRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepositoryResult> AddAsync(PricingRule rule, CancellationToken cancellationToken = default);
    Task<RepositoryResult> UpdateAsync(PricingRule rule, CancellationToken cancellationToken = default);
    Task<RepositoryResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class JsonRuleRepository : IRuleRepository, IDisposable
{
    private readonly List<PricingRule> _rules = [];
    private readonly string _filePath;
    private readonly ILogger<JsonRuleRepository> _logger;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> PathGates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonRuleRepository(IConfiguration configuration, ILogger<JsonRuleRepository> logger)
        : this(configuration["RuleStorage:Path"] ?? "data/rules.json", logger,
            configuration.GetValue("RuleStorage:SeedWhenMissing", true))
    { }

    public JsonRuleRepository(
        string filePath,
        ILogger<JsonRuleRepository> logger,
        bool seedWhenMissing = true)
    {
        _filePath = Path.GetFullPath(filePath);
        _gate = PathGates.GetOrAdd(_filePath, _ => new SemaphoreSlim(1, 1));
        _logger = logger;
        Load(seedWhenMissing);
    }

    public async Task<IReadOnlyList<PricingRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { ReloadLocked(); return _rules.ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { ReloadLocked(); return _rules.FirstOrDefault(rule => rule.Id == id); }
        finally { _gate.Release(); }
    }

    public Task<RepositoryResult> AddAsync(PricingRule rule, CancellationToken cancellationToken = default) =>
        MutateAsync(() =>
        {
            if (_rules.Any(existing => existing.Id == rule.Id))
                return new(false, RepositoryFailure.Conflict, $"Rule with id '{rule.Id}' already exists.");
            if (_rules.Any(existing => NameEquals(existing.Name, rule.Name)))
                return new(false, RepositoryFailure.Conflict, $"Rule with name '{rule.Name}' already exists.");
            _rules.Add(rule);
            return new(true);
        }, cancellationToken);

    public Task<RepositoryResult> UpdateAsync(PricingRule rule, CancellationToken cancellationToken = default) =>
        MutateAsync(() =>
        {
            var index = _rules.FindIndex(existing => existing.Id == rule.Id);
            if (index < 0) return new(false, RepositoryFailure.NotFound, "Rule was not found.");
            if (_rules.Any(existing => existing.Id != rule.Id && NameEquals(existing.Name, rule.Name)))
                return new(false, RepositoryFailure.Conflict, $"Rule with name '{rule.Name}' already exists.");
            _rules[index] = rule;
            return new(true);
        }, cancellationToken);

    public Task<RepositoryResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _rules.RemoveAll(rule => rule.Id == id) == 0
            ? new(false, RepositoryFailure.NotFound, "Rule was not found.") : new(true), cancellationToken);

    private async Task<RepositoryResult> MutateAsync(
        Func<RepositoryResult> mutation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ReloadLocked();
            var before = _rules.ToArray();
            var result = mutation();
            if (!result.Success) return result;
            try
            {
                await PersistLockedAsync(cancellationToken);
                return result;
            }
            catch (Exception exception)
            {
                _rules.Clear(); _rules.AddRange(before);
                _logger.LogError(exception, "Failed to persist pricing rules to {RuleFilePath}", _filePath);
                return new(false, RepositoryFailure.Persistence, "Rules could not be persisted.");
            }
        }
        finally { _gate.Release(); }
    }

    private async Task PersistLockedAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(_rules, JsonOptions),
            cancellationToken);
        File.Move(temporaryPath, _filePath, true);
    }

    private void Load(bool seedWhenMissing)
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _rules.AddRange(
                    JsonSerializer.Deserialize<List<PricingRule>>(json, JsonOptions) ?? []);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to load pricing rules from {RuleFilePath}",
                    _filePath);
            }
        }
        if (seedWhenMissing) Seed();
    }

    private void ReloadLocked()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<List<PricingRule>>(
                File.ReadAllText(_filePath),
                JsonOptions) ?? [];
            _rules.Clear();
            _rules.AddRange(loaded);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                exception,
                "Failed to reload pricing rules; retaining the last valid snapshot at {RuleFilePath}",
                _filePath);
        }
    }

    private void Seed() => _rules.AddRange([
        new WeightTierRule
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Local Parcel (0-5kg)",
            MinWeight = 0,
            MaxWeight = 5,
            PricePerKg = 20m,
            Priority = 10,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new WeightTierRule
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Standard Parcel (5-20kg)",
            MinWeight = 5,
            MaxWeight = 20,
            PricePerKg = 25m,
            Priority = 20,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new WeightTierRule
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Name = "Heavy Parcel (20-100kg)",
            MinWeight = 20,
            MaxWeight = 100,
            PricePerKg = 30m,
            Priority = 30,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new WeightTierRule
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Name = "Freight (100kg and above)",
            MinWeight = 100,
            MaxWeight = null,
            PricePerKg = 35m,
            Priority = 40,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new RemoteAreaSurchargeRule
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Name = "Bangkok Metropolitan Region Surcharge",
            SurchargeAmount = 30m,
            Area = "Bangkok Metropolitan Region",
            Priority = 50,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new RemoteAreaSurchargeRule
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Name = "Other Provinces Surcharge",
            SurchargeAmount = 80m,
            Area = "Other Provinces",
            Priority = 60,
            EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new TimeWindowPromotionRule
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "July 2026 Demo Promotion",
            DiscountPercentage = 5,
            Priority = 70,
            EffectiveFrom = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc)
        }
    ]);

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    public void Dispose() { }
}
