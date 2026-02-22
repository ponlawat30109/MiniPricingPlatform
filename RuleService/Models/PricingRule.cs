using System.Text.Json.Serialization;

namespace RuleService.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleType
{
    TimeWindowPromotion,
    RemoteAreaSurcharge,
    WeightTier
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(TimeWindowPromotionRule), typeDiscriminator: "TimeWindowPromotion")]
[JsonDerivedType(typeof(RemoteAreaSurchargeRule), typeDiscriminator: "RemoteAreaSurcharge")]
[JsonDerivedType(typeof(WeightTierRule), typeDiscriminator: "WeightTier")]
public abstract class PricingRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public RuleType Type { get; init; }
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TimeWindowPromotionRule : PricingRule
{
    public TimeWindowPromotionRule() => Type = RuleType.TimeWindowPromotion;
    public double DiscountPercentage { get; set; }
    public TimeOnly? FromTime { get; set; }
    public TimeOnly? ToTime { get; set; }
}

public class RemoteAreaSurchargeRule : PricingRule
{
    public RemoteAreaSurchargeRule() => Type = RuleType.RemoteAreaSurcharge;
    public decimal SurchargeAmount { get; set; }
    public string? Area { get; set; }
}

public class WeightTierRule : PricingRule
{
    public WeightTierRule() => Type = RuleType.WeightTier;
    public double MinWeight { get; set; }
    public double MaxWeight { get; set; }
    public decimal PricePerKg { get; set; }
}
