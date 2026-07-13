using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Options;
using PricingService.Models;
using PricingService.Services;

namespace PricingService.Endpoints;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/quotes/price", Price);
        app.MapPost("/quotes/bulk", SubmitBulk).DisableAntiforgery();
        app.MapGet("/jobs/{job_id}", (string job_id, JobManager jobs) => jobs.GetJob(job_id) is { } job
            ? Results.Ok(job) : Problem(404, "Job not found", "No job exists with the supplied identifier."));
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return app;
    }

    private static async Task<IResult> Price(QuoteRequest request, IPricingEngine engine,
        IRuleServiceClient rules, CancellationToken cancellationToken)
    {
        var issues = QuoteValidator.Validate(request);
        if (issues.Count != 0) return ValidationProblem(issues);
        try { return Results.Ok(engine.CalculatePrice(request, await rules.GetRulesAsync(cancellationToken))); }
        catch (HttpRequestException)
        {
            return Problem(
                503,
                "Rule Service unavailable",
                "Pricing rules are temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            return Problem(
                502,
                "Invalid Rule Service response",
                "The Rule Service returned an invalid response.");
        }
    }

    private static async Task<IResult> SubmitBulk(HttpRequest request, JobManager jobs,
        IOptions<BulkJobOptions> configured, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        BulkJobRequest? bulk;
        if (request.HasJsonContentType())
        {
            try { bulk = await request.ReadFromJsonAsync<BulkJobRequest>(cancellationToken); }
            catch (Exception exception) when (exception is System.Text.Json.JsonException or BadHttpRequestException)
            { return Problem(400, "Invalid request", "The JSON body is invalid."); }
        }
        else if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files["file"];
            if (file is null || file.Length == 0)
            {
                return Problem(
                    400,
                    "Invalid request",
                    "A non-empty CSV file named 'file' is required.");
            }
            if (file.Length > options.MaxCsvBytes)
            {
                return Problem(
                    413,
                    "CSV too large",
                    $"CSV files are limited to {options.MaxCsvBytes} bytes.");
            }
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                bulk = new() { Quotes = csv.GetRecords<QuoteRequest>().Take(options.MaxItems + 1).ToList() };
            }
            catch (CsvHelperException exception)
            {
                var row = exception.Context?.Parser?.Row ?? 0;
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [$"row {row}"] = ["The CSV row contains an invalid value."]
                }, title: "Invalid CSV", statusCode: 400);
            }
            catch (Exception)
            {
                return Problem(400, "Invalid CSV", "The CSV could not be parsed.");
            }
        }
        else return Problem(415, "Unsupported content type", "Use application/json or multipart/form-data.");

        if (bulk is null) return Problem(400, "Invalid request", "A request body is required.");
        if (bulk.Quotes.Count == 0) return Problem(400, "Invalid request", "At least one quote is required.");
        if (bulk.Quotes.Count > options.MaxItems)
        {
            return Problem(
                400,
                "Bulk limit exceeded",
                $"At most {options.MaxItems} quotes are allowed.");
        }
        var rowErrors = bulk.Quotes
            .Select((quote, index) => (index, issues: QuoteValidator.Validate(quote)))
            .Where(row => row.issues.Count != 0)
            .SelectMany(row => row.issues.Select(issue => new ValidationIssue(
                $"quotes[{row.index}].{issue.Field}",
                issue.Message)))
            .ToList();
        if (rowErrors.Count != 0) return ValidationProblem(rowErrors);
        if (!jobs.TryCreateJob(bulk, out var id))
        {
            return Problem(
                503,
                "Queue full",
                "The bulk queue is at capacity. Try again later.");
        }
        return Results.Accepted($"/jobs/{id}", new BulkAcceptedResponse(id));
    }

    private static IResult ValidationProblem(IEnumerable<ValidationIssue> issues) => Results.ValidationProblem(
        issues
            .GroupBy(issue => issue.Field)
            .ToDictionary(
                group => group.Key,
                group => group.Select(issue => issue.Message).ToArray()),
        title: "Validation failed", statusCode: 400);

    private static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}
