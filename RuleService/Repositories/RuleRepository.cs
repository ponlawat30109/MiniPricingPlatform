using System.Text.Json;
using System.Text.Json.Serialization;
using RuleService.Models;

namespace RuleService.Repositories;

public interface IRuleRepository
{
    Task<IEnumerable<PricingRule>> GetAllAsync();
    Task<PricingRule?> GetByIdAsync(Guid id);
    Task<(bool Success, string? Error)> AddAsync(PricingRule rule);
    Task UpdateAsync(PricingRule rule);
    Task DeleteAsync(Guid id);
}

public class JsonRuleRepository : IRuleRepository
{
    private readonly List<PricingRule> _rules = [];
    private readonly string _filePath = "data/rules.json";
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonRuleRepository()
    {
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var rules = JsonSerializer.Deserialize<List<PricingRule>>(json, _jsonOptions);
                if (rules != null && rules.Count > 0)
                {
                    _rules.AddRange(rules);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RuleRepository] Error loading rules.json: {ex.Message}. Falling back to seed data.");
            }
        }

        SeedData();
        PersistAsync().GetAwaiter().GetResult();
    }

    private void SeedData()
    {
        _rules.AddRange([
            new TimeWindowPromotionRule
            {
                Name = "Holiday Special 20% Off",
                DiscountPercentage = 20,
                Priority = 1,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                IsActive = true
            },
            new RemoteAreaSurchargeRule
            {
                Name = "Extended Area Surcharge",
                SurchargeAmount = 150.0m,
                Area = "Islands & Mountains",
                Priority = 5,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new WeightTierRule
            {
                Name = "Base Delivery Rate (0-1000kg)",
                MinWeight = 0,
                MaxWeight = 1000,
                PricePerKg = 2.5m,
                Priority = 10,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        ]);
    }

    private async Task PersistAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_rules, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RuleRepository] Error saving rules.json: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public Task<IEnumerable<PricingRule>> GetAllAsync() =>
        Task.FromResult(_rules.AsEnumerable());

    public Task<PricingRule?> GetByIdAsync(Guid id) =>
        Task.FromResult(_rules.FirstOrDefault(r => r.Id == id));

    public async Task<(bool Success, string? Error)> AddAsync(PricingRule rule)
    {
        if (_rules.Any(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase)))
            return (false, $"Rule with name '{rule.Name}' already exists.");

        _rules.Add(rule);
        await PersistAsync();
        return (true, null);
    }

    public async Task UpdateAsync(PricingRule rule)
    {
        var index = _rules.FindIndex(r => r.Id == rule.Id);
        if (index >= 0)
        {
            _rules[index] = rule;
            await PersistAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        _rules.RemoveAll(r => r.Id == id);
        await PersistAsync();
    }
}


