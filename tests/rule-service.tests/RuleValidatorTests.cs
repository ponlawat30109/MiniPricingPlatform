using RuleService.Application;
using RuleService.Models;

namespace RuleService.Tests;

public class RuleValidatorTests
{
    [Fact]
    public void OpenEndedWeightTierIsValid()
    {
        var rule = new WeightTierRule
        {
            Name = "Heavy freight",
            MinWeight = 100,
            MaxWeight = null,
            PricePerKg = 35,
            EffectiveFrom = DateTime.UtcNow
        };
        Assert.Empty(RuleValidator.Validate(rule));
    }

    [Fact]
    public void WeightTierRequiresPositivePrice()
    {
        var rule = new WeightTierRule
        {
            Name = "Free freight",
            MinWeight = 0,
            MaxWeight = 5,
            PricePerKg = 0,
            EffectiveFrom = DateTime.UtcNow
        };
        Assert.Contains(nameof(WeightTierRule.PricePerKg), RuleValidator.Validate(rule).Keys);
    }
}
