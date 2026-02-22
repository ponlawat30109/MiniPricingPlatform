using PricingService.Models;
using PricingService.Services;
using CsvHelper;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("RuleService", client =>
{
    // This will be overridden by environment variables in Docker
    client.BaseAddress = new Uri(builder.Configuration["RuleService:BaseUrl"] ?? "http://localhost:5000");
});

builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<IPricingEngine, PricingEngine>();
builder.Services.AddHostedService<BulkWorker>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();
app.UseCors();

// Pricing Endpoints
app.MapPost("/quotes/price", async (QuoteRequest request, IPricingEngine engine, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("RuleService");
    var rules = await client.GetFromJsonAsync<List<PricingRuleDto>>("/rules") ?? [];
    
    return Results.Ok(engine.CalculatePrice(request, rules));
});

app.MapPost("/quotes/bulk", async (HttpRequest request, JobManager jobManager, ILogger<Program> logger) =>
{
    if (request.HasJsonContentType())
    {
        var bulkRequest = await request.ReadFromJsonAsync<BulkJobRequest>();
        if (bulkRequest == null) return Results.BadRequest("Invalid JSON.");
        var jobId = jobManager.CreateJob(bulkRequest);
        return Results.Accepted($"/jobs/{jobId}", new { job_id = jobId });
    }
    else if (request.HasFormContentType)
    {
        var file = request.Form.Files["file"];
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded with name 'file'.");

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        try
        {
            var records = csv.GetRecords<QuoteRequest>().ToList();
            if (records.Count == 0) return Results.BadRequest("CSV file is empty or headers do not match.");
            
            var jobId = jobManager.CreateJob(new BulkJobRequest { Quotes = records });
            return Results.Accepted($"/jobs/{jobId}", new { job_id = jobId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CSV Parsing Error");
            return Results.BadRequest($"Error parsing CSV: {ex.Message}");
        }
    }
    return Results.BadRequest("Unsupported Content-Type. Use application/json or multipart/form-data.");
}).DisableAntiforgery();

app.MapGet("/jobs/{job_id}", (string job_id, JobManager jobManager) =>
    jobManager.GetJob(job_id) is { } job ? Results.Ok(job) : Results.NotFound());

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public partial class Program { }
