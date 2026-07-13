using Microsoft.Extensions.Options;
using PricingService.Models;
using PricingService.Services;

namespace PricingService.Tests;

public class JobManagerTests
{
    [Fact]
    public void TryCreateJob_RejectsWhenBoundedQueueIsFull()
    {
        var manager = CreateManager(capacity: 1);
        Assert.True(manager.TryCreateJob(Request(), out _));
        Assert.False(manager.TryCreateJob(Request(), out _));
    }

    [Fact]
    public void Fail_ExposesTypedStatusAndFailureDetails()
    {
        var manager = CreateManager();
        manager.TryCreateJob(Request(), out var id);
        manager.Fail(id, "rules_unavailable", "Pricing rules could not be loaded.");

        var job = manager.GetJob(id)!;
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("rules_unavailable", job.Failure!.Code);
        Assert.Null(job.Request);
    }

    [Fact]
    public void Cleanup_RemovesCompletedJobsPastRetention()
    {
        var now = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var manager = CreateManager(retention: TimeSpan.FromMinutes(5), clock: () => now);
        manager.TryCreateJob(Request(), out var id);
        manager.Complete(id, []);
        now = now.AddMinutes(6);

        Assert.Equal(1, manager.RemoveExpired());
        Assert.Null(manager.GetJob(id));
    }

    [Fact]
    public void Cancel_MarksActiveJobFailedWithDetailsAndFinishedTime()
    {
        var now = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
        var manager = CreateManager(clock: () => now);
        manager.TryCreateJob(Request(), out var id);
        manager.Start(id);

        manager.Cancel(id);

        var job = manager.GetJob(id)!;
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("cancelled", job.Failure!.Code);
        Assert.Equal(now, job.FinishedAt);
        Assert.Null(job.Request);
    }

    private static JobManager CreateManager(int capacity = 4, TimeSpan? retention = null,
        Func<DateTimeOffset>? clock = null) => new(Options.Create(new BulkJobOptions
        {
            QueueCapacity = capacity,
            Retention = retention ?? TimeSpan.FromHours(1)
        }), clock ?? (() => DateTimeOffset.UtcNow));

    private static BulkJobRequest Request() => new() { Quotes = [new() { Weight = 1, Area = "City" }] };
}
