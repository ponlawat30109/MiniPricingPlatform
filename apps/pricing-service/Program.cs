using PricingService.Endpoints;
using PricingService.Models;
using PricingService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.Configure<BulkJobOptions>(builder.Configuration.GetSection("BulkJobs"));
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<IPricingEngine, PricingEngine>();
builder.Services.AddHttpClient<IRuleServiceClient, RuleServiceClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["RuleService:BaseUrl"] ?? "http://localhost:5000"));
builder.Services.AddHostedService<BulkWorker>();
builder.Services.AddHostedService<JobCleanupWorker>();
var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200", "http://127.0.0.1:4200"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapPricingEndpoints();
app.Run();

public partial class Program { }
