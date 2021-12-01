using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Channels;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations;
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
        private readonly ConcurrentDictionary<string, DownloadJobBatch> _downloadJobBatches = new();

        public DownloadBrokerService(ILogger<DownloadBrokerService> logger, JobQueueChannel jobQueue,
            IServiceProvider serviceProvider, IOptionsMonitor<DownloadHandlerOptions> optionsSnapshot,
            IOptionsMonitor<ResolvedUploadTarget>? resolvedUploadTargetOpts)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _serviceProvider = serviceProvider;
            _optionsSnapshot = optionsSnapshot;
            _resolvedUploadTargetOpts = resolvedUploadTargetOpts;
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
                CompletedDownloadJob completedDownloadJob =
                    await (processor?.HandleDownloadJobAsync(downloadJob, cancellationToken) ??
                           throw new NullReferenceException());
                foreach (string dlJobAssociatedUploadConfigId in dlJob.AssociatedUploadConfigIds)
                {
                    var resolvedUploadTarget = _resolvedUploadTargetOpts?.Get(dlJobAssociatedUploadConfigId)
                        .CreateUploadJob(completedDownloadJob.DownloadedFile);
                    var added = await _jobQueue.AddUploadJobAsync(
                        resolvedUploadTarget ?? throw new InvalidOperationException(),
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