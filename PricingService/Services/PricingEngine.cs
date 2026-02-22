using PricingService.Models;

namespace PricingService.Services;

public interface IPricingEngine
{
    QuoteResponse CalculatePrice(QuoteRequest request, IEnumerable<PricingRuleDto> rules);
}

public class PricingEngine : IPricingEngine
{
    public QuoteResponse CalculatePrice(QuoteRequest request, IEnumerable<PricingRuleDto> rules)
    {
        // Sort rules by priority (lower number = higher priority)
        var sortedRules = rules
            .Where(r => r.IsActive && 
                        r.EffectiveFrom <= request.RequestDate && 
                        (r.EffectiveTo == null || r.EffectiveTo >= request.RequestDate))
            .OrderBy(r => r.Priority);

        decimal rawPrice = 0;
        decimal surcharges = 0;
        decimal discounts = 0;
        List<string> appliedRules = [];

        foreach (var rule in sortedRules)
        {
            switch (rule)
            {
                case { Type: RuleType.WeightTier } when rule.MinWeight <= request.Weight && (rule.MaxWeight is null || rule.MaxWeight >= request.Weight):
                    var weightPrice = (decimal)request.Weight * (rule.PricePerKg ?? 0);
                    rawPrice += weightPrice;
                    appliedRules.Add($"Applied Weight Tier: {rule.Name} (+฿{weightPrice:N2})");
                    break;

                case { Type: RuleType.RemoteAreaSurcharge } when (request.Area != null && rule.Area != null && (rule.Area.Contains(request.Area, StringComparison.OrdinalIgnoreCase) || request.Area.Contains(rule.Area, StringComparison.OrdinalIgnoreCase) || rule.Area == "Default")):
                    surcharges += rule.SurchargeAmount ?? 0;
                    appliedRules.Add($"Applied Surcharge: {rule.Name} (+฿{rule.SurchargeAmount:N2})");
                    break;

                case { Type: RuleType.TimeWindowPromotion }:
                    var now = TimeOnly.FromDateTime(request.RequestDate);

                    if (rule.FromTime == null || rule.ToTime == null || (now >= rule.FromTime && now <= rule.ToTime))
                    {
                        var discount = (rawPrice + surcharges) * (decimal)((rule.DiscountPercentage ?? 0) / 100.0);
                        discounts += discount;
                        appliedRules.Add($"Applied Promotion: {rule.Name} (-฿{discount:N2})");
                    }
                    break;
            }
        }

        return new QuoteResponse
        {
            BasePrice = rawPrice,
            Surcharges = surcharges,
            Discounts = discounts,
            TotalPrice = rawPrice + surcharges - discounts,
            AppliedRules = appliedRules
        };
    }
}
