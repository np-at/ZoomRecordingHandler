using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Channels;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.BackgroundServices
{
    public class DownloadHandlerOptions
    {
        public Type? DownloadJobHandlerType { get; set; }
    }

    public class DownloadBrokerService : BackgroundService
    {
        private readonly ILogger<DownloadBrokerService> _logger;
        private readonly JobQueueChannel _jobQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<DownloadHandlerOptions> _optionsSnapshot;
        private readonly IOptionsMonitor<ResolvedUploadTarget>? _resolvedUploadTargetOpts;
        private readonly IJobTaskInstanceRepository _taskInstanceRepository;

        public DownloadBrokerService(ILogger<DownloadBrokerService> logger, JobQueueChannel jobQueue,
            IServiceProvider serviceProvider, IOptionsMonitor<DownloadHandlerOptions> optionsSnapshot,
            IOptionsMonitor<ResolvedUploadTarget>? resolvedUploadTargetOpts, IJobTaskInstanceRepository taskInstanceRepository)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _serviceProvider = serviceProvider;
            _optionsSnapshot = optionsSnapshot;
            _resolvedUploadTargetOpts = resolvedUploadTargetOpts;
            _taskInstanceRepository = taskInstanceRepository;
        }


        private async Task ConsumeJobFromQueueAsync(CancellationToken cancellationToken = default)
        {
            DownloadJobBatch dlJob = await _jobQueue.ReadDownloadJobBatchAsync(cancellationToken);

            var associatedConfig = _optionsSnapshot.Get(dlJob.AssociatedDownloadConfigId);
            var type = associatedConfig?.DownloadJobHandlerType ??
                       throw new ArgumentOutOfRangeException(nameof(dlJob.AssociatedDownloadConfigId));


            using var scope = _serviceProvider.CreateScope();

            var processor = scope.ServiceProvider.GetRequiredService(type) as IDownloadJobHandler;
            if (dlJob.Jobs == null)
                throw new ArgumentNullException(nameof(dlJob.Jobs));
            if (processor == null)
                throw new ArgumentException(
                    "Unable to find service of type: {Type} to fulfill IDownloadJobHandler requirement", nameof(type));

            foreach (var downloadJob in dlJob.Jobs)
            {
                CompletedDownloadJob completedDownloadJob;
                try
                {
                    await _taskInstanceRepository.UpdateStatusForTaskAsync(downloadJob.Id,
                        TaskInstanceStatus.InProgress).ConfigureAwait(false);
                    var dlJobTask = (processor?.HandleDownloadJobAsync(downloadJob, cancellationToken) ??
                                     throw new NullReferenceException());
                    completedDownloadJob = await dlJobTask;
                    completedDownloadJob.DownloadJobId = downloadJob.Id;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing download job");
                    await _taskInstanceRepository.UpdateStatusForTaskAsync(downloadJob.Id, TaskInstanceStatus.Failed);
                    throw;
                }

                await _taskInstanceRepository.UpdateStatusForTaskAsync(downloadJob.Id, TaskInstanceStatus.Finished);
                foreach (string dlJobAssociatedUploadConfigId in dlJob.AssociatedUploadConfigIds)
                {
                    var resolvedUploadTarget = _resolvedUploadTargetOpts?
                            .Get(dlJobAssociatedUploadConfigId)
                        ;
                    var resolvedUploadJob = resolvedUploadTarget?.CreateUploadJob(completedDownloadJob);

                    var newTaskInstance = new JobTaskInstance
                    {
                        Status = TaskInstanceStatus.Pending,
                        JobType = resolvedUploadJob?.UploadTargetConfig?.Type switch
                        {
                            JobType.Sharepoint => JobTaskType.UploadSharepoint,
                            JobType.OnedriveUser => JobTaskType.UploadOnedriveUser,
                            JobType.OnedriveDrive => JobTaskType.UploadOnedriveDrive,
                            JobType.Dropbox => JobTaskType.UploadDropbox,
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        ParentJob = downloadJob.ParentJobId
                    };
                    await _taskInstanceRepository.Create(newTaskInstance, cancellationToken);
                    resolvedUploadJob.AssociatedJobTaskInstanceId = newTaskInstance.Id;
                    var added = await _jobQueue.AddUploadJobAsync(
                        resolvedUploadJob ?? throw new InvalidOperationException(),
                        cancellationToken);
                    if (!added)
                        throw new InvalidOperationException();
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ConsumeJobFromQueueAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogWarning(ex, "operations cancelled");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while consuming job from queue");
                        // await _processingChannel.AddZoomEventAsync(webhookEvent, stoppingToken).ConfigureAwait(false);

                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation Cancelled exception occured");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occurred");
                // _jobQueue.TryCompleteWriter(ex);
            }
            // finally
            // {
            //     _processingChannel.TryCompleteWriter();
            // }
        }
    }
}