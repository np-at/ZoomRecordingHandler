using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Json.Path;
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

        public IEnumerable<string> AllowedAuthorizationHeaderValues { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> ValidationTests { get; set; } = new(0);
    }

    [GenericTypeControllerFeatureProvider.GenericControllerNameAttribute]
    public class GenericReceiverController<T> : ControllerBase
    {
        private readonly ILogger<GenericReceiverController<T>> _logger;
        private readonly IWebhookDownloadJobTransformer<T> _jobTransformer;

        private readonly GenericReceiverControllerOptions<T> _options;

        private readonly IEnumerable<string> _configIds;
        private readonly IJobRepository _jobRepository;
        private readonly IJobTaskInstanceRepository _jobTaskRepository;
        private readonly JobQueueChannel _jobQueue;


        public GenericReceiverController(ILogger<GenericReceiverController<T>> logger,
            IWebhookDownloadJobTransformer<T> jobTransformer, JobQueueChannel jobQueue,
            IOptions<GenericReceiverControllerOptions<T>> options, IJobRepository jobRepository,
            IJobTaskInstanceRepository jobTaskRepository)
        {
            _logger = logger;
            _jobTransformer = jobTransformer;
            _options = options.Value;
            _jobRepository = jobRepository;
            _jobTaskRepository = jobTaskRepository;
            _jobQueue = jobQueue;
            _configIds = options.Value.ConfigIds;
        }

        private bool PerformCustomValidation(string jsonString)
        {
            // Skip validation if there's no tests to perform
            if (!_options.ValidationTests.Any())
                return true;

            using var jsonDoc = JsonDocument.Parse(jsonString);
            foreach (var optionsValidationTest in _options.ValidationTests)
            {
                optionsValidationTest.Deconstruct(out string jsonPath, out string pattern);

                // Find the value of the object at the location of the specified JSONPath
                var evaluationResult = JsonPath.Parse(jsonPath)?.Evaluate(jsonDoc.RootElement);
                if (evaluationResult?.Error != null)
                    throw new Exception(evaluationResult.Error);
                if (evaluationResult?.Matches?.Count > 1)
                    throw new Exception($"More than one result found for JSONPath validation expression: {jsonPath}");
                // We're assuming that the resolved value is a string (will throw an exception if not)
                string? match = evaluationResult?.Matches?[0].Value.GetString();

                // Check for a pattern match against the resolved value 
                if (!Regex.IsMatch(match ?? string.Empty, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
                    return false;
            }

            return true;
        }


        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] T body,
            [FromHeader(Name = "Authorization")] string? authKey, CancellationToken cancellationToken = default)
        {
            if (_options.AllowedAuthorizationHeaderValues.Any() &&
                !_options.AllowedAuthorizationHeaderValues
                    .Contains(authKey)) //.Any(s => s.Equals(authKey, StringComparison.Ordinal)))
            {
                return new UnauthorizedResult();
            }

            // we need the raw string value of the request body for JSONPath validation and to store for later in the db.

            // This feels wasteful to me, There's undoubtedly a better way of obtaining it than serializing the just deserialized object but ¯\_(ツ)_/¯ 
            var jsonBody = JsonSerializer.Serialize(body);
            
            
            if (!PerformCustomValidation(jsonBody))
            {
                _logger.LogDebug("Webhook failed Custom validation");
            }


#if DEBUG
            // This is just for debugging,  avoid the object allocation otherwise 
            _logger.LogWarning("body: {@Body}", body);
#endif
            // TODO: Clean this shit up
            try
            {
                var jobTrackingEntry = new Job
                {
                    Name = string.Empty,
                    RawMessage = jsonBody,
                    Source = nameof(T),
                    AssociatedReceiverId = _options.AssociatedReceiverId ?? throw new Exception()
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

                var batch = new DownloadJobBatch
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