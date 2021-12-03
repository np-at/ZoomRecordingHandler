using System;
using System.Collections.Generic;
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
    public class OnedriveUserProvider : BaseGraphClientProvider
    {
        private readonly ILogger<OnedriveUserProvider> _logger;
        private readonly IOptionsSnapshot<OD_UserClientConfig> _oneDriveClientConfig;

        public OnedriveUserProvider(ILogger<OnedriveUserProvider> logger, IOptionsSnapshot<OD_UserClientConfig> oneDriveClientConfig) : base(logger, oneDriveClientConfig)
        {
            _logger = logger;
            _oneDriveClientConfig = oneDriveClientConfig;
        }


        protected override async Task<IUploadSession> CreateUploadSession(ResolvedUploadJob resolvedUploadJob, CancellationToken cancellationToken = default)

        {
            var gs = CreateGraphClient(resolvedUploadJob);

            string? conflictBehavior = resolvedUploadJob.UploadTarget?.FileExistsBehavior switch
            {
                FileExistsBehavior.Rename => "rename",
                FileExistsBehavior.Unknown => throw new ArgumentNullException(),
                FileExistsBehavior.Overwrite => "replace",
                FileExistsBehavior.Error => "fail",
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
            var userName = resolvedUploadJob.UploadTargetConfig?.ClientConfig?.UserName ?? throw new ArgumentNullException(nameof(resolvedUploadJob.UploadTargetConfig.ClientConfig.UserName));

            
            return await gs.Users[userName]
                .Drive.Root.ItemWithPath(itemPath)
                .CreateUploadSession(uploadProps)
                .Request()
                .PostAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        
    }
}