using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Models;
using ZFHandler.Models.ConfigurationSchemas;
using BindingFlags = System.Reflection.BindingFlags;

namespace ZFHandler.Services
{
    public class BrokerServiceOptions
    {
        public UploadTargetConfig[]? UploadTargetConfigs { get; set; }
        public IEnumerable<UploadTarget>? UploadTargets { get; set; }
    }


    public class BrokerService : INotificationHandler<DownloadJobBatch>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BrokerService> _logger;
        private readonly BrokerServiceOptions _options;

        public BrokerService(IMediator mediator, ILogger<BrokerService> logger, IOptions<BrokerServiceOptions> options)
        {
            _mediator = mediator;
            _logger = logger;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            Console.WriteLine(options?.Value);
        }

        private UploadTargetConfig? GetUploadTargetConfig(UploadTarget uploadTarget)
        {
            return _options?.UploadTargetConfigs?.Where(x => x.Identifier == uploadTarget.ConfigId).FirstOrDefault();
        }

        public async Task Handle(DownloadJobBatch notification, CancellationToken cancellationToken)
        {
            JobTracker.ConcurrentDictionary.TryAdd(notification.Id, notification);

            foreach (var downloadJob in JobTracker.ConcurrentDictionary.GetOrAdd(notification.Id, notification).Jobs ??
                                        Array.Empty<DownloadJob>())
            {
                downloadJob.ParentJobId = notification.Id;
                downloadJob.Status = JobStatus.NotStarted;
                JobTracker.DownloadJobs.TryAdd(downloadJob.Id, downloadJob);
                var dlTask = _mediator.Send(downloadJob, cancellationToken).ConfigureAwait(false);
                dlTask.GetAwaiter().OnCompleted(() =>
                {
                    if (JobTracker.DownloadJobs.TryGetValue(downloadJob.Id, out var dictJob))
                        if (!dictJob.Equals(downloadJob))
                            _logger.LogError(
                                "JobTracker entry for DownloadJob does not match local representation: {@DownloadJob}, {@DictJob}",
                                downloadJob, dictJob);
                });
                var result = await dlTask;
                _logger.LogInformation("Download Job Completed with result: {JobResult}", result);

                // TODO: Deal with file upload errors
                if (result == null)
                    throw new NotImplementedException("Dealing with file download errors is on the TODO");
                else
                    if (JobTracker.DownloadJobs.TryGetValue(downloadJob.Id, out var refreshedJob))
                        await DispatchUploadJobsFromCompletedDownloadAsync(refreshedJob, cancellationToken)
                            .ConfigureAwait(false);
                    else
                    {
                        _logger.LogError("Can't find referenced job");
                    }
            }
        }

        private async Task DispatchUploadJobsFromCompletedDownloadAsync(DownloadJob downloadJob,
            CancellationToken ct = default)
        {
            if (_options.UploadTargets == null) throw new ArgumentNullException(nameof(_options.UploadTargets));
            if (!File.Exists(downloadJob.DestinationFile?.FullName))
                throw new ArgumentNullException(nameof(downloadJob.DestinationFile));
            
            foreach (var uploadTarget in _options.UploadTargets)
            {
                var config = GetUploadTargetConfig(uploadTarget);


                switch (config?.Type ?? throw new ArgumentNullException(nameof(config)))
                {
                    case JobType.Sharepoint:
                        var uploadJob =
                            generateUploadJobSpecFromDownloadJob<SharepointClientConfig>(downloadJob, uploadTarget,
                                config);
                        await _mediator.Publish(uploadJob, ct).ConfigureAwait(false);
                        _logger.LogDebug("Dispatching upload job {@UploadJob}", uploadJob);

                        break;
                    case JobType.OnedriveUser:
                        var uploadJob2 =
                            generateUploadJobSpecFromDownloadJob<OD_UserClientConfig>(downloadJob, uploadTarget,
                                config);
                        await _mediator.Publish(uploadJob2, ct).ConfigureAwait(false);
                        _logger.LogDebug("Dispatching upload job {@UploadJob}", uploadJob2);
                        break;
                    case JobType.OnedriveDrive:
                        var uploadJob3 =
                            generateUploadJobSpecFromDownloadJob<OD_DriveClientConfig>(downloadJob, uploadTarget,
                                config);
                        await _mediator.Publish(uploadJob3, ct).ConfigureAwait(false);
                        _logger.LogDebug("Dispatching upload job {@UploadJob}", uploadJob3);
                        break;
                    case JobType.Dropbox:
                        var uploadJob4 =
                            generateUploadJobSpecFromDownloadJob<DropBoxClientConfig>(downloadJob, uploadTarget,
                                config);
                        await _mediator.Publish(uploadJob4, ct).ConfigureAwait(false);
                        _logger.LogDebug("Dispatching upload job {@UploadJob}", uploadJob4);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private UploadJobSpec<T> generateUploadJobSpecFromDownloadJob<T>(DownloadJob downloadJob,
            UploadTarget uploadTarget, UploadTargetConfig uploadTargetConfig) where T : BaseClientConfig
        {
            var uploadJobSpec = new UploadJobSpec<T>()
            {
                FileInfo = downloadJob.DestinationFile ?? throw new InvalidOperationException(),
                Id = Guid.NewGuid().ToString("N"),
                Status = JobStatus.Null,
            };
            uploadJobSpec.TargetName = !string.IsNullOrWhiteSpace(uploadTarget.NamingTemplate)
                ? Strings.Format(uploadJobSpec.FileInfo.Name, uploadTarget.NamingTemplate)
                : uploadJobSpec.FileInfo.Name;
            uploadJobSpec.TargetDir = !string.IsNullOrWhiteSpace(uploadTarget.DirectoryNamingTemplate)
                ? throw new NotImplementedException()
                : uploadJobSpec.FileInfo.DirectoryName ?? "DIR";
            return uploadJobSpec;
        }

      
    }
}