using System.Collections.Concurrent;
using System.Threading.Channels;
using PricingService.Models;

namespace PricingService.Services;

public class JobManager
{
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly Channel<string> _jobChannel = Channel.CreateUnbounded<string>();

    public string CreateJob(BulkJobRequest request)
    {
        var jobId = Guid.NewGuid().ToString();
        _jobs[jobId] = new JobStatusResponse 
        { 
            JobId = jobId, 
            Status = "Pending",
            Request = request
        };
        
        _jobChannel.Writer.TryWrite(jobId);
        return jobId;
    }

    public JobStatusResponse? GetJob(string jobId) => _jobs.GetValueOrDefault(jobId);

    public ChannelReader<string> GetReader() => _jobChannel.Reader;

    public void UpdateJob(string jobId, string status, List<QuoteResponse>? results = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = status;
            if (results != null) job.Results = results;
        }
    }
}
