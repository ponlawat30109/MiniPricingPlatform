using PricingService.Models;
using PricingService.Services;

namespace PricingService.Tests;

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();
    private static readonly DateTime Now = new(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CalculatePrice_UsesOnlyLowestPriorityMatchingWeightTier()
    {
        var selectedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var result = _engine.CalculatePrice(Quote(10),
        [
            Rule("fallback", RuleType.WeightTier, 20, price: 100),
            Rule("selected", RuleType.WeightTier, 1, price: 5) with { Id = selectedId },
            Rule("same priority tie", RuleType.WeightTier, 1, price: 99) with
            {
                Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")
            }
        ]);

        Assert.Equal(50m, result.BasePrice);
        Assert.Single(result.AppliedRules);
        Assert.Contains("selected", result.AppliedRules[0]);
        Assert.Contains("฿50.00", result.AppliedRules[0]);
    }

    [Fact]
    public void CalculatePrice_AppliesAllSurchargesThenEachPromotionToSubtotal()
    {
        var result = _engine.CalculatePrice(Quote(10, "North"),
        [
            Rule("tier", RuleType.WeightTier, 10, price: 10),
            Rule("zone one", RuleType.RemoteAreaSurcharge, 20, amount: 20, area: "North"),
            Rule("zone two", RuleType.RemoteAreaSurcharge, 1, amount: 30, area: "North"),
            Rule("ten", RuleType.TimeWindowPromotion, 30, percent: 10),
            Rule("twenty", RuleType.TimeWindowPromotion, 2, percent: 20)
        ]);

        Assert.Equal(100m, result.BasePrice);
        Assert.Equal(50m, result.Surcharges);
        Assert.Equal(45m, result.Discounts);
        Assert.Equal(105m, result.TotalPrice);
        Assert.Equal(["tier", "zone two", "zone one", "twenty", "ten"],
            result.AppliedRules.Select(DescriptionName));
    }

    [Fact]
    public void CalculatePrice_FiltersInactiveFutureAndExpiredRules()
    {
        var valid = Rule("valid", RuleType.WeightTier, 4, price: 3);
        var result = _engine.CalculatePrice(Quote(2),
        [
            valid,
            Rule("inactive", RuleType.RemoteAreaSurcharge, 1, amount: 100, area: "Default")
                with { IsActive = false },
            Rule("future", RuleType.RemoteAreaSurcharge, 1, amount: 100, area: "Default")
                with { EffectiveFrom = Now.AddMinutes(1) },
            Rule("expired", RuleType.RemoteAreaSurcharge, 1, amount: 100, area: "Default")
                with { EffectiveTo = Now.AddMinutes(-1) }
        ]);

        Assert.Equal(6m, result.TotalPrice);
        Assert.Single(result.AppliedRules);
    }

    [Fact]
    public void CalculatePrice_UsesOpenEndedTierForHeavyShipment()
    {
        var result = _engine.CalculatePrice(Quote(250, "Other Provinces"),
        [
            Rule("heavy", RuleType.WeightTier, 40, price: 35) with { MinWeight = 100, MaxWeight = null },
            Rule("province", RuleType.RemoteAreaSurcharge, 50, amount: 80, area: "Other Provinces")
        ]);

        Assert.Equal(8750m, result.BasePrice);
        Assert.Equal(80m, result.Surcharges);
        Assert.Equal(8830m, result.TotalPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_RejectsNonPositiveOrNonFiniteWeight(double weight)
    {
        var errors = QuoteValidator.Validate(Quote(weight));
        Assert.Contains(errors, error => error.Field == "weight");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_RejectsBlankArea(string area)
    {
        var errors = QuoteValidator.Validate(Quote(1, area));
        Assert.Contains(errors, error => error.Field == "area");
    }

    private static QuoteRequest Quote(double weight, string area = "City") =>
        new() { Weight = weight, Area = area, RequestDate = Now };

    private static PricingRuleDto Rule(string name, RuleType type, int priority,
        decimal? price = null, decimal? amount = null, double? percent = null, string? area = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Priority = priority,
            EffectiveFrom = Now.AddDays(-1),
            IsActive = true,
            MinWeight = 0,
            PricePerKg = price,
            SurchargeAmount = amount,
            DiscountPercentage = percent,
            Area = area
        };

    private static string DescriptionName(string description) =>
        description.Split(':', 2)[1].Split('(', 2)[0].Trim();
}
