namespace PricingService.Models;

using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;

public class QuoteRequest
{
    [Name("weight", "Weight")]
    public required double Weight { get; init; }
    [Name("area", "Area")]
    public required string Area { get; init; }
    [Ignore]
    public DateTime RequestDate { get; init; } = DateTime.UtcNow;
}

public class QuoteResponse
{
    public decimal BasePrice { get; init; }
    public decimal Surcharges { get; init; }
    public decimal Discounts { get; init; }
    public decimal TotalPrice { get; init; }
    public List<string> AppliedRules { get; init; } = [];
}

public class BulkJobRequest
{
    public required List<QuoteRequest> Quotes { get; init; } = [];
}

public class JobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed
    public BulkJobRequest? Request { get; set; }
    public List<QuoteResponse>? Results { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleType
{
    TimeWindowPromotion,
    RemoteAreaSurcharge,
    WeightTier
}

public class PricingRuleDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public RuleType Type { get; init; }
    public int Priority { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsActive { get; init; }

    public double? DiscountPercentage { get; init; }
    public decimal? SurchargeAmount { get; init; }
    public double? MinWeight { get; init; }
    public double? MaxWeight { get; init; }
    public decimal? PricePerKg { get; init; }
    public string? Area { get; init; }
    public TimeOnly? FromTime { get; init; }
    public TimeOnly? ToTime { get; init; }
}
