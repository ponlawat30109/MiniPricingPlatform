using Microsoft.AspNetCore.Mvc;
using RuleService.Models;
using RuleService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRuleRepository, JsonRuleRepository>();
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

// Rule Endpoints
var rulesApi = app.MapGroup("/rules");

rulesApi.MapGet("/", async (IRuleRepository repo) => 
    Results.Ok(await repo.GetAllAsync()));

rulesApi.MapGet("/{id}", async (Guid id, IRuleRepository repo) =>
{
    var rule = await repo.GetByIdAsync(id);
    return rule is not null ? Results.Ok(rule) : Results.NotFound();
});

rulesApi.MapPost("/promotion", async ([FromBody] TimeWindowPromotionRule rule, IRuleRepository repo) =>
{
    var (success, error) = await repo.AddAsync(rule);
    return success ? Results.Created($"/rules/{rule.Id}", rule) : Results.Conflict(new { error });
});

rulesApi.MapPost("/surcharge", async ([FromBody] RemoteAreaSurchargeRule rule, IRuleRepository repo) =>
{
    var (success, error) = await repo.AddAsync(rule);
    return success ? Results.Created($"/rules/{rule.Id}", rule) : Results.Conflict(new { error });
});

rulesApi.MapPost("/weight-tier", async ([FromBody] WeightTierRule rule, IRuleRepository repo) =>
{
    var (success, error) = await repo.AddAsync(rule);
    return success ? Results.Created($"/rules/{rule.Id}", rule) : Results.Conflict(new { error });
});

rulesApi.MapDelete("/{id}", async (Guid id, IRuleRepository repo) =>
{
    await repo.DeleteAsync(id);
    return Results.NoContent();
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public partial class Program { }
