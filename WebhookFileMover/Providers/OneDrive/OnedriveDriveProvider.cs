using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Providers.OneDrive
{
    public class OnedriveDriveProvider : BaseGraphClientProvider
    {
        private readonly ILogger<OnedriveDriveProvider> _logger;
        private readonly IOptionsSnapshot<OD_DriveClientConfig> _optionsSnapshot;

        public OnedriveDriveProvider(ILogger<OnedriveDriveProvider> logger,
            IOptionsSnapshot<OD_DriveClientConfig> optionsSnapshot) : base(logger, optionsSnapshot)
        {
            _logger = logger;
            _optionsSnapshot = optionsSnapshot;
        }
        
        // {
        //     // var currentOptions = _optionsSnapshot.Get(configId) ?? throw new KeyNotFoundException();
        //    
        //    
        //    // var itemPath = uploadJobSpec.GetRelativePath();
        //
        //
        //  
        //     // await PUplaod(uploadJobSpec.SourceFile, itemPath, configId, null, cancellationToken);
        // }


        protected override async Task<IUploadSession> CreateUploadSession(ResolvedUploadJob resolvedUploadJob,
            CancellationToken cancellationToken = default)
        {
            var gs = CreateGraphClient(resolvedUploadJob);
            // var currentOptions = _optionsSnapshot.Get(configId) ?? throw new KeyNotFoundException();
            // string? uploadTarget = currentOptions.DriveId ??
            //                        throw new ArgumentNullException(nameof(currentOptions.DriveId));
            // string? itemPath = formattedRelativeItemPathWithName ??
            //                    throw new ArgumentNullException(nameof(formattedRelativeItemPathWithName));
            string? conflictBehavior = resolvedUploadJob.UploadTarget?.FileExistsBehavior switch
            {
                FileExistsBehavior.Rename => "rename",
                FileExistsBehavior.Unknown => throw new ArgumentNullException(),
                FileExistsBehavior.Overwrite => "replace",
                FileExistsBehavior.Error => throw new IOException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            var uploadProps = new DriveItemUploadableProperties()
            {
                ODataType = null,
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", conflictBehavior }
                }
            };
            var itemPath = resolvedUploadJob.GetRelativePath();
            var driveId = (resolvedUploadJob.UploadTargetConfig?.ClientConfig as OD_DriveClientConfig)?.DriveId ??
                          throw new ArgumentNullException(nameof(resolvedUploadJob.UploadTargetConfig.ClientConfig
                              .DriveId));

            return await gs.Drives[driveId]
                .Root.ItemWithPath(itemPath)
                .CreateUploadSession(uploadProps)
                .Request()
                .PostAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}