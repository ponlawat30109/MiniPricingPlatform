namespace PricingService.Models;

using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;

public sealed class QuoteRequest
{
    [Name("weight", "Weight")]
    public required double Weight { get; init; }
    [Name("area", "Area")]
    public required string Area { get; init; }
    [Ignore]
    public DateTime RequestDate { get; init; } = DateTime.UtcNow;
}

public sealed class QuoteResponse
{
    public decimal BasePrice { get; init; }
    public decimal Surcharges { get; init; }
    public decimal Discounts { get; init; }
    public decimal TotalPrice { get; init; }
    public IReadOnlyList<string> AppliedRules { get; init; } = [];
}

public sealed class BulkJobRequest
{
    public required List<QuoteRequest> Quotes { get; init; } = [];
}

public sealed record BulkAcceptedResponse([property: JsonPropertyName("job_id")] string JobId);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus { Pending, Processing, Completed, Failed }

public sealed record JobFailure(string Code, string Message);

public sealed class JobStatusResponse
{
    public required string JobId { get; init; }
    public JobStatus Status { get; internal set; }
    [JsonIgnore]
    public BulkJobRequest? Request { get; internal set; }
    public IReadOnlyList<QuoteResponse>? Results { get; internal set; }
    public JobFailure? Failure { get; internal set; }
    [JsonIgnore]
    public DateTimeOffset? FinishedAt { get; internal set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleType { TimeWindowPromotion, RemoteAreaSurcharge, WeightTier }

public record PricingRuleDto
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

public sealed record ValidationIssue(string Field, string Message);

public sealed class BulkJobOptions
{
    public int QueueCapacity { get; set; } = 100;
    public int MaxItems { get; set; } = 1000;
    public long MaxCsvBytes { get; set; } = 1_048_576;
    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
