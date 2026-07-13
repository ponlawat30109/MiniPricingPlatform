using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PricingService.Models;

namespace PricingService.Services;

public sealed class JobManager
{
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly Channel<string> _channel;
    private readonly BulkJobOptions _options;
    private readonly Func<DateTimeOffset> _clock;

    public JobManager(IOptions<BulkJobOptions> options)
        : this(options, () => DateTimeOffset.UtcNow)
    {
    }
    public JobManager(IOptions<BulkJobOptions> options, Func<DateTimeOffset> clock)
    {
        _options = options.Value;
        _clock = clock;
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(_options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public bool TryCreateJob(BulkJobRequest request, out string jobId)
    {
        jobId = Guid.NewGuid().ToString("N");
        var job = new JobStatusResponse
        {
            JobId = jobId,
            Status = JobStatus.Pending,
            Request = request
        };
        if (!_jobs.TryAdd(jobId, job)) return false;
        if (_channel.Writer.TryWrite(jobId)) return true;
        _jobs.TryRemove(jobId, out _);
        return false;
    }

    public JobStatusResponse? GetJob(string id) => _jobs.GetValueOrDefault(id);
    public ChannelReader<string> Reader => _channel.Reader;
    public void Start(string id)
    {
        if (_jobs.TryGetValue(id, out var job))
            job.Status = JobStatus.Processing;
    }

    public void Complete(string id, IReadOnlyList<QuoteResponse> results)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        job.Status = JobStatus.Completed;
        job.Results = results;
        job.Request = null;
        job.FinishedAt = _clock();
    }

    public void Fail(string id, string code, string message)
    {
        if (!_jobs.TryGetValue(id, out var job)) return;
        job.Status = JobStatus.Failed;
        job.Failure = new(code, message);
        job.Request = null;
        job.FinishedAt = _clock();
    }

    public void Cancel(string id) =>
        Fail(id, "cancelled", "The bulk job was cancelled because the service is stopping.");
    public int RemoveExpired()
    {
        var cutoff = _clock() - _options.Retention;
        return _jobs.Count(pair => pair.Value.FinishedAt < cutoff && _jobs.TryRemove(pair.Key, out _));
    }
}
