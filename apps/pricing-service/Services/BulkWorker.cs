using PricingService.Models;

namespace PricingService.Services;

public sealed class BulkWorker(JobManager jobs, IPricingEngine engine, IRuleServiceClient rulesClient,
    ILogger<BulkWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in jobs.Reader.ReadAllAsync(stoppingToken))
        {
            jobs.Start(id);
            try
            {
                var rules = await rulesClient.GetRulesAsync(stoppingToken);
                var job = jobs.GetJob(id);
                if (job?.Request is null) continue;
                List<QuoteResponse> results = [];
                foreach (var quote in job.Request.Quotes)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    results.Add(engine.CalculatePrice(quote, rules));
                }
                jobs.Complete(id, results);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                jobs.Cancel(id);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Bulk job {JobId} failed", id);
                jobs.Fail(id, "processing_failed", "The bulk job could not be completed.");
            }
        }
    }
}

public sealed class JobCleanupWorker(
    JobManager jobs,
    Microsoft.Extensions.Options.IOptions<BulkJobOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) jobs.RemoveExpired();
    }
}
