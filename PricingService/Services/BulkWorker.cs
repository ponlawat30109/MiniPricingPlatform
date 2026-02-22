using PricingService.Models;

namespace PricingService.Services;

public class BulkWorker(JobManager jobManager, IPricingEngine pricingEngine, IHttpClientFactory httpClientFactory, ILogger<BulkWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = jobManager.GetReader();

        await foreach (var jobId in reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation("Processing job {JobId}", jobId);
            jobManager.UpdateJob(jobId, "Processing");

            try
            {
                var client = httpClientFactory.CreateClient("RuleService");
                var rules = await client.GetFromJsonAsync<List<PricingRuleDto>>("/rules", stoppingToken) ?? [];

                var job = jobManager.GetJob(jobId);
                if (job?.Request is null) continue;

                List<QuoteResponse> results = [];
                foreach (var quoteRequest in job.Request.Quotes)
                {
                    results.Add(pricingEngine.CalculatePrice(quoteRequest, rules));
                }

                jobManager.UpdateJob(jobId, "Completed", results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing job {JobId}", jobId);
                jobManager.UpdateJob(jobId, "Failed");
            }
        }
    }
}
