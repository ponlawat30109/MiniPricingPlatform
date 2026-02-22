using PricingService.Models;
using PricingService.Services;
using Xunit;

namespace PricingService.Tests;

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();

    [Fact]
    public void CalculatePrice_WeightTier_AppliesCorrectPrice()
    {
        // Arrange
        var request = new QuoteRequest { Weight = 15, Area = "City" };
        var rules = new List<PricingRuleDto>
        {
            new PricingRuleDto
            {
                Name = "Heavy Tier",
                Type = RuleType.WeightTier,
                MinWeight = 10,
                MaxWeight = 100,
                PricePerKg = 5,
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            }
        };

        // Act
        var result = _engine.CalculatePrice(request, rules);

        // Assert
        Assert.Equal(75, result.TotalPrice); // 15 * 5 = 75
        Assert.Contains("Heavy Tier", result.AppliedRules[0]);
    }

    [Fact]
    public void CalculatePrice_Surcharge_AppliesCorrectAmount()
    {
        // Arrange
        var request = new QuoteRequest { Weight = 5, Area = "RemoteArea" };
        var rules = new List<PricingRuleDto>
        {
            new PricingRuleDto
            {
                Name = "RemoteArea",
                Type = RuleType.RemoteAreaSurcharge,
                SurchargeAmount = 50,
                Area = "RemoteArea",
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            }
        };

        // Act
        var result = _engine.CalculatePrice(request, rules);

        // Assert
        Assert.Equal(50, result.TotalPrice);
        Assert.Equal(50, result.Surcharges);
    }

    [Fact]
    public void CalculatePrice_Promotion_AppliesDiscount()
    {
        // Arrange
        var request = new QuoteRequest { Weight = 10, Area = "City" };
        var rules = new List<PricingRuleDto>
        {
            new PricingRuleDto
            {
                Name = "Base Rate",
                Type = RuleType.WeightTier,
                MinWeight = 0,
                PricePerKg = 10,
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            },
            new PricingRuleDto
            {
                Name = "10% Off",
                Type = RuleType.TimeWindowPromotion,
                DiscountPercentage = 10,
                Priority = 2,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            }
        };

        // Act
        var result = _engine.CalculatePrice(request, rules);

        // Assert
        // Base = 10 * 10 = 100
        // Discount = 100 * 0.1 = 10
        // Total = 90
        Assert.Equal(90, result.TotalPrice);
        Assert.Equal(10, result.Discounts);
    }
}
