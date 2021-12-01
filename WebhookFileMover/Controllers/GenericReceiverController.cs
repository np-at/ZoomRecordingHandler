using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Channels;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Controllers
{
    public class GenericReceiverControllerOptions
    {
        public IEnumerable<string> ConfigIds { get; set; } = Array.Empty<string>();
    }

    [Route("api/[controller]")]
    [GenericTypeControllerFeatureProvider.GenericControllerNameAttribute]
    public class GenericReceiverController<T> : ControllerBase
    {
        private readonly ILogger<GenericReceiverController<T>> _logger;
        private readonly IWebhookDownloadJobTransformer<T> _jobTransformer;
        private readonly JobQueueChannel _jobQueue;
        private readonly IEnumerable<string> _configIds;
        

        public GenericReceiverController(ILogger<GenericReceiverController<T>> logger,
            IWebhookDownloadJobTransformer<T> jobTransformer, JobQueueChannel jobQueue,
            IOptions<GenericReceiverControllerOptions> options)
        {
            _logger = logger;
            _jobTransformer = jobTransformer;
            _jobQueue = jobQueue;
            _configIds = options.Value.ConfigIds;
            
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] T body, CancellationToken cancellationToken = default)
        {
#if DEBUG
            _logger.LogWarning("body: {@Body}", body);
#endif
            try
            {
                var dlJobs = await _jobTransformer.TransformWebhook(body, cancellationToken);
                var batch = new DownloadJobBatch()
                {
                    Jobs = dlJobs,
                    AssociatedUploadConfigIds = _configIds
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