using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Channels;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Controllers
{
    public class GenericReceiverControllerOptions<T>
    {
        public string? AssociatedReceiverId { get; set; }
        public IEnumerable<string> ConfigIds { get; set; } = Array.Empty<string>();
    }

    // [Route("api/[controller]")]
    [GenericTypeControllerFeatureProvider.GenericControllerNameAttribute]
    public class GenericReceiverController<T> : ControllerBase
    {
        
        private readonly ILogger<GenericReceiverController<T>> _logger;
        private readonly IWebhookDownloadJobTransformer<T> _jobTransformer;

        private readonly IOptions<GenericReceiverControllerOptions<T>> _options;

        // private readonly JobQueueChannel _jobQueue;
        private readonly IEnumerable<string> _configIds;
        private readonly IJobRepository _jobRepository;
        private readonly IJobTaskInstanceRepository _jobTaskRepository;
        

        public GenericReceiverController(ILogger<GenericReceiverController<T>> logger,
            IWebhookDownloadJobTransformer<T> jobTransformer, JobQueueChannel jobQueue,
            IOptions<GenericReceiverControllerOptions<T>> options, IJobRepository jobRepository, IJobTaskInstanceRepository jobTaskRepository)
        {
            
            _logger = logger;
            _jobTransformer = jobTransformer;
            _options = options;
            _jobRepository = jobRepository;
            _jobTaskRepository = jobTaskRepository;
            // _jobQueue = jobQueue;
            _configIds = options.Value.ConfigIds;
            
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] T body,[FromServices] JobQueueChannel _jobQueue, CancellationToken cancellationToken = default)
        {
            
#if DEBUG
            _logger.LogWarning("body: {@Body}", body);
#endif
            try
            {
                var jobTrackingEntry = new Job()
                {
                    Name = String.Empty,
                    RawMessage = System.Text.Json.JsonSerializer.Serialize<T>(body),
                    Source = nameof(T),
                    AssociatedReceiverId = _options?.Value?.AssociatedReceiverId ?? throw new Exception()
                    
                };
                await _jobRepository.Create(jobTrackingEntry);
                var dlJobs = await _jobTransformer.TransformWebhook(body, cancellationToken);
                var downloadJobs = dlJobs as DownloadJob[] ?? dlJobs.ToArray();
                foreach (var downloadJob in downloadJobs)
                {
                    downloadJob.ParentJobId = jobTrackingEntry.Id;
                    var jobTaskInstance = new JobTaskInstance
                    {
                        Status = TaskInstanceStatus.Pending,
                        ParentJob = jobTrackingEntry.Id,
                        JobType = JobTaskType.Download
                    };
                    await _jobTaskRepository.Create(jobTaskInstance, cancellationToken).ConfigureAwait(false);
                    downloadJob.Id = jobTaskInstance.Id;
                }

                var batch = new DownloadJobBatch()
                {
                    Jobs = downloadJobs,
                    AssociatedUploadConfigIds = _configIds,
                    JobTrackingId = jobTrackingEntry.Id
                };
                bool added = await _jobQueue.AddDownloadJobBatchAsync(batch, cancellationToken);

                if (!added)
                    throw new ApplicationException("unable to add job to queue");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while processing incoming webhook");
                throw;
            }

            return new AcceptedResult();
        }
    }
}