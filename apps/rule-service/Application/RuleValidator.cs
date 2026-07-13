using RuleService.Models;

namespace RuleService.Application;

public static class RuleValidator
{
    public static Dictionary<string, string[]> Validate(PricingRule rule)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors[key] = [message];
        if (string.IsNullOrWhiteSpace(rule.Name))
            Add(nameof(rule.Name), "Name is required.");
        if (rule.EffectiveTo is not null && rule.EffectiveTo < rule.EffectiveFrom)
        {
            Add(
                nameof(rule.EffectiveTo),
                "EffectiveTo must be on or after EffectiveFrom.");
        }
        switch (rule)
        {
            case TimeWindowPromotionRule promotion:
                if (!double.IsFinite(promotion.DiscountPercentage) ||
                    promotion.DiscountPercentage is < 0 or > 100)
                {
                    Add(
                        nameof(promotion.DiscountPercentage),
                        "DiscountPercentage must be between 0 and 100.");
                }
                if (promotion.FromTime is not null &&
                    promotion.ToTime is not null &&
                    promotion.ToTime <= promotion.FromTime)
                {
                    Add(nameof(promotion.ToTime), "ToTime must be after FromTime.");
                }
                break;
            case RemoteAreaSurchargeRule surcharge:
                if (surcharge.SurchargeAmount < 0)
                    Add(nameof(surcharge.SurchargeAmount), "SurchargeAmount cannot be negative.");
                if (string.IsNullOrWhiteSpace(surcharge.Area))
                    Add(nameof(surcharge.Area), "Area is required.");
                break;
            case WeightTierRule tier:
                if (!double.IsFinite(tier.MinWeight) || tier.MinWeight < 0)
                {
                    Add(
                        nameof(tier.MinWeight),
                        "MinWeight must be finite and nonnegative.");
                }
                if (tier.MaxWeight is not null &&
                    (!double.IsFinite(tier.MaxWeight.Value) || tier.MaxWeight <= tier.MinWeight))
                {
                    Add(
                        nameof(tier.MaxWeight),
                        "MaxWeight must be finite and greater than MinWeight when supplied.");
                }
                if (tier.PricePerKg <= 0)
                    Add(nameof(tier.PricePerKg), "PricePerKg must be positive.");
                break;
        }
        return errors;
    }
}
