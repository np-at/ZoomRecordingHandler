using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Channels;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Models.Interfaces;
using WebhookFileMover.Providers.Dropbox;
using WebhookFileMover.Providers.OneDrive;

namespace WebhookFileMover.BackgroundServices
{
    public class UploadBrokerService : BackgroundService
    {
        private readonly ILogger<UploadBrokerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JobQueueChannel _jobQueueChannel;
        
        public UploadBrokerService(ILogger<UploadBrokerService> logger, IServiceProvider serviceProvider, JobQueueChannel jobQueueChannel)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _jobQueueChannel = jobQueueChannel;
        }

        private async Task ConsumeUploadJobAsync(CancellationToken cancellationToken)
        {
            ResolvedUploadJob uploadJob = await _jobQueueChannel.ReadUploadJobAsync(cancellationToken);
            using var scope = _serviceProvider.CreateScope();

            IBaseUploadProvider processor = uploadJob.UploadTargetConfig.Type switch
            {
                JobType.Sharepoint => scope.ServiceProvider.GetRequiredService<SharepointProvider>(),
                JobType.OnedriveUser => scope.ServiceProvider.GetRequiredService<OnedriveUserProvider>(),
                JobType.OnedriveDrive => scope.ServiceProvider.GetRequiredService<OnedriveDriveProvider>(),
                JobType.Dropbox => scope.ServiceProvider.GetRequiredService<DropboxProvider>(),
                _ => throw new ArgumentOutOfRangeException()
            };
            await processor.UploadFileAsync(uploadJob, cancellationToken).ConfigureAwait(false);
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