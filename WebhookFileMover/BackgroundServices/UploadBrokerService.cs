using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Channels;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Models.Interfaces;
using WebhookFileMover.Providers.Dropbox;
using WebhookFileMover.Providers.OneDrive;
using WebhookFileMover.Services;

namespace WebhookFileMover.BackgroundServices
{
    public class UploadBrokerService : BackgroundService
    {
        private readonly ILogger<UploadBrokerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JobQueueChannel _jobQueueChannel;
        private readonly IJobTaskInstanceRepository _taskInstanceRepository;
        private readonly INotificationEvaluator _notificationEvaluator;
        private readonly IJobTaskInstanceProvider _jobTaskInstanceProvider;

        public UploadBrokerService(ILogger<UploadBrokerService> logger, IServiceProvider serviceProvider,
            JobQueueChannel jobQueueChannel, IJobTaskInstanceRepository taskInstanceRepository, INotificationEvaluator notificationEvaluator, IJobTaskInstanceProvider jobTaskInstanceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _jobQueueChannel = jobQueueChannel;
            _taskInstanceRepository = taskInstanceRepository;
            _notificationEvaluator = notificationEvaluator;
            _jobTaskInstanceProvider = jobTaskInstanceProvider;
        }

        private async Task ConsumeUploadJobAsync(CancellationToken cancellationToken)
        {
            ResolvedUploadJob uploadJob = await _jobQueueChannel.ReadUploadJobAsync(cancellationToken);
            await _taskInstanceRepository
                .UpdateStatusForTaskAsync(uploadJob.AssociatedJobTaskInstanceId, TaskInstanceStatus.InProgress)
                .ConfigureAwait(false);
            using var scope = _serviceProvider.CreateScope();

            IBaseUploadProvider processor = uploadJob.UploadTargetConfig?.Type switch
            {
                JobType.Sharepoint => scope.ServiceProvider.GetRequiredService<SharepointProvider>(),
                JobType.OnedriveUser => scope.ServiceProvider.GetRequiredService<OnedriveUserProvider>(),
                JobType.OnedriveDrive => scope.ServiceProvider.GetRequiredService<OnedriveDriveProvider>(),
                JobType.Dropbox => scope.ServiceProvider.GetRequiredService<DropboxProvider>(),
                _ => throw new ArgumentOutOfRangeException()
            };
            try
            {

                var uploadTask = processor.UploadFileAsync(uploadJob, cancellationToken).ConfigureAwait(false);
                var location = await uploadTask;
                JobTaskInstance jobTaskInstance = await _jobTaskInstanceProvider.GetSingle(uploadJob.AssociatedJobTaskInstanceId, cancellationToken);
                jobTaskInstance.LocationUri = location?.ToString();
                jobTaskInstance.Status = TaskInstanceStatus.Finished;
                await _taskInstanceRepository.Update(jobTaskInstance, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while attempting to process upload job {@Job}", uploadJob);
                await _taskInstanceRepository.UpdateStatusForTaskAsync(uploadJob.AssociatedJobTaskInstanceId,
                    TaskInstanceStatus.Failed);
                throw;
            }
        
            await _notificationEvaluator.HandleFinishedTaskAsync(uploadJob.AssociatedJobTaskInstanceId);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ConsumeUploadJobAsync(stoppingToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogWarning(ex, "operations cancelled");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while processing upload job from queue");
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error while processing upload job from queue");
                throw;
            }
        }
    }
}