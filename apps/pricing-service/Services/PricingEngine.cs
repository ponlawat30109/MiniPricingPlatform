using PricingService.Models;

namespace PricingService.Services;

public interface IPricingEngine
{
    QuoteResponse CalculatePrice(QuoteRequest request, IEnumerable<PricingRuleDto> rules);
}

public static class QuoteValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(QuoteRequest request)
    {
        List<ValidationIssue> issues = [];
        if (!double.IsFinite(request.Weight) || request.Weight <= 0)
            issues.Add(new("weight", "Weight must be a positive finite number."));
        if (string.IsNullOrWhiteSpace(request.Area))
            issues.Add(new("area", "Area is required."));
        return issues;
    }
}

public sealed class PricingEngine : IPricingEngine
{
    public QuoteResponse CalculatePrice(QuoteRequest request, IEnumerable<PricingRuleDto> rules)
    {
        var active = rules
            .Where(rule => rule.IsActive &&
                rule.EffectiveFrom <= request.RequestDate &&
                (rule.EffectiveTo is null || rule.EffectiveTo >= request.RequestDate))
            .ToList();
        List<string> descriptions = [];

        var tier = active
            .Where(rule => rule.Type == RuleType.WeightTier &&
                rule.MinWeight.GetValueOrDefault() <= request.Weight &&
                (rule.MaxWeight is null || rule.MaxWeight >= request.Weight))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .FirstOrDefault();
        var basePrice = tier is null ? 0 : (decimal)request.Weight * tier.PricePerKg.GetValueOrDefault();
        if (tier is not null) descriptions.Add($"Applied Weight Tier: {tier.Name} (+฿{basePrice:N2})");

        var surcharges = 0m;
        foreach (var rule in active
            .Where(rule => rule.Type == RuleType.RemoteAreaSurcharge &&
                AreaMatches(rule.Area, request.Area))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase))
        {
            var amount = rule.SurchargeAmount.GetValueOrDefault();
            surcharges += amount;
            descriptions.Add($"Applied Surcharge: {rule.Name} (+฿{amount:N2})");
        }

        var subtotal = basePrice + surcharges;
        var discounts = 0m;
        foreach (var rule in active
            .Where(rule => rule.Type == RuleType.TimeWindowPromotion &&
                TimeMatches(rule, request.RequestDate))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase))
        {
            var discount = subtotal * (decimal)(rule.DiscountPercentage.GetValueOrDefault() / 100d);
            discounts += discount;
            descriptions.Add($"Applied Promotion: {rule.Name} (-฿{discount:N2})");
        }

        return new()
        {
            BasePrice = basePrice,
            Surcharges = surcharges,
            Discounts = discounts,
            TotalPrice = subtotal - discounts,
            AppliedRules = descriptions
        };
    }

    private static bool AreaMatches(string? configured, string requested) =>
        configured is not null &&
        (configured.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
            configured.Equals(requested.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool TimeMatches(PricingRuleDto rule, DateTime requested) =>
        rule.FromTime is null ||
        rule.ToTime is null ||
        TimeOnly.FromDateTime(requested) >= rule.FromTime &&
        TimeOnly.FromDateTime(requested) <= rule.ToTime;
}
