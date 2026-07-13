using System.Text.Json.Serialization;
using System.Text.Json;

namespace RuleService.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleType { TimeWindowPromotion, RemoteAreaSurcharge, WeightTier }

[JsonConverter(typeof(PricingRuleJsonConverter))]
public abstract record PricingRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public RuleType Type { get; init; }
    public int Priority { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class PricingRuleJsonConverter : JsonConverter<PricingRule>
{
    public override PricingRule? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty("Type", out var type) &&
            !document.RootElement.TryGetProperty("type", out type))
            throw new JsonException("A rule Type is required.");
        var target = Enum.Parse<RuleType>(type.GetString()!, true) switch
        {
            RuleType.TimeWindowPromotion => typeof(TimeWindowPromotionRule),
            RuleType.RemoteAreaSurcharge => typeof(RemoteAreaSurchargeRule),
            RuleType.WeightTier => typeof(WeightTierRule),
            _ => throw new JsonException("Unknown rule Type.")
        };
        return (PricingRule?)JsonSerializer.Deserialize(
            document.RootElement.GetRawText(),
            target,
            options);
    }

    public override void Write(Utf8JsonWriter writer, PricingRule value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

public sealed record TimeWindowPromotionRule : PricingRule
{
    public TimeWindowPromotionRule() => Type = RuleType.TimeWindowPromotion;
    public double DiscountPercentage { get; init; }
    public TimeOnly? FromTime { get; init; }
    public TimeOnly? ToTime { get; init; }
}

public sealed record RemoteAreaSurchargeRule : PricingRule
{
    public RemoteAreaSurchargeRule() => Type = RuleType.RemoteAreaSurcharge;
    public decimal SurchargeAmount { get; init; }
    public string? Area { get; init; }
}

public sealed record WeightTierRule : PricingRule
{
    public WeightTierRule() => Type = RuleType.WeightTier;
    public double MinWeight { get; init; }
    public double? MaxWeight { get; init; }
    public decimal PricePerKg { get; init; }
}
