using Microsoft.AspNetCore.Mvc;
using RuleService.Application;
using RuleService.Models;
using RuleService.Repositories;
using System.Text.Json;

namespace RuleService.Endpoints;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rules = endpoints.MapGroup("/rules");
        rules.MapGet("/", async (IRuleRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.GetAllAsync(ct)));
        rules.MapGet("/{id}", GetById);
        rules.MapPost(
            "/promotion",
            (TimeWindowPromotionRule rule, IRuleRepository repo, CancellationToken ct) =>
                Create(rule, repo, ct));
        rules.MapPost(
            "/surcharge",
            (RemoteAreaSurchargeRule rule, IRuleRepository repo, CancellationToken ct) =>
                Create(rule, repo, ct));
        rules.MapPost(
            "/weight-tier",
            (WeightTierRule rule, IRuleRepository repo, CancellationToken ct) =>
                Create(rule, repo, ct));
        rules.MapPut("/{id}", Update);
        rules.MapDelete("/{id}", Delete);
        return endpoints;
    }

    private static async Task<IResult> GetById(
        string id,
        IRuleRepository repository,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId)) return InvalidId();
        var rule = await repository.GetByIdAsync(parsedId, cancellationToken);
        return rule is null ? NotFound() : Results.Ok(rule);
    }

    private static async Task<IResult> Create(
        PricingRule rule,
        IRuleRepository repository,
        CancellationToken cancellationToken)
    {
        if (RuleValidator.Validate(rule) is { Count: > 0 } errors)
            return Results.ValidationProblem(errors);
        var result = await repository.AddAsync(rule, cancellationToken);
        return result.Success ? Results.Created($"/rules/{rule.Id}", rule) : Failure(result);
    }

    private static async Task<IResult> Update(
        string id,
        HttpRequest request,
        IRuleRepository repository,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId)) return InvalidId();
        PricingRule? rule;
        try
        {
            using var document = await JsonDocument.ParseAsync(
                request.Body,
                cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("type", out var typeElement) &&
                !document.RootElement.TryGetProperty("Type", out typeElement))
                return Results.Problem(statusCode: 400, title: "Rule type is required");

            var json = document.RootElement.GetRawText();
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            rule = typeElement.GetString() switch
            {
                nameof(RuleType.TimeWindowPromotion) =>
                    JsonSerializer.Deserialize<TimeWindowPromotionRule>(json, options),
                nameof(RuleType.RemoteAreaSurcharge) =>
                    JsonSerializer.Deserialize<RemoteAreaSurchargeRule>(json, options),
                nameof(RuleType.WeightTier) =>
                    JsonSerializer.Deserialize<WeightTierRule>(json, options),
                _ => null
            };
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "Invalid rule payload");
        }
        if (rule is null) return Results.Problem(statusCode: 400, title: "Rule payload is required");
        rule = rule with { Id = parsedId };
        if (RuleValidator.Validate(rule) is { Count: > 0 } errors)
            return Results.ValidationProblem(errors);
        var result = await repository.UpdateAsync(rule, cancellationToken);
        return result.Success ? Results.Ok(rule) : Failure(result);
    }

    private static async Task<IResult> Delete(
        string id,
        IRuleRepository repository,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId)) return InvalidId();
        var result = await repository.DeleteAsync(parsedId, cancellationToken);
        return result.Success || result.Failure == RepositoryFailure.NotFound
            ? Results.NoContent()
            : Failure(result);
    }

    private static IResult Failure(RepositoryResult result) => result.Failure switch
    {
        RepositoryFailure.Conflict => Results.Problem(
            statusCode: 409,
            title: "Rule conflict",
            detail: result.Error),
        RepositoryFailure.NotFound => NotFound(),
        _ => Results.Problem(
            statusCode: 500,
            title: "Rule persistence failed",
            detail: "Rules could not be persisted.")
    };

    private static IResult NotFound() => Results.Problem(statusCode: 404, title: "Rule not found");
    private static IResult InvalidId() => Results.Problem(statusCode: 400, title: "Invalid rule id");
}
