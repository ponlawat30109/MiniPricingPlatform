using RuleService.Endpoints;
using RuleService.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IRuleRepository, JsonRuleRepository>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration
        .GetSection("Cors:Origins")
        .Get<string[]>() ?? ["http://localhost:4200", "http://127.0.0.1:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    await Results.Problem(statusCode: response.StatusCode).ExecuteAsync(context.HttpContext);
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapRuleEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.Run();

public partial class Program { }
