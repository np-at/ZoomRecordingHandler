using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Providers.OneDrive
{
    public class SharepointProvider : BaseGraphClientProvider
    {
        private readonly ILogger<SharepointProvider> _logger;
        private readonly IOptionsSnapshot<SharepointClientConfig> _optionsSnapshot;

        public SharepointProvider(ILogger<SharepointProvider> logger, IOptionsSnapshot<SharepointClientConfig> optionsSnapshot) : base(logger, optionsSnapshot)
        {
            _logger = logger;
            _optionsSnapshot = optionsSnapshot;
        }

     

        internal override async  Task<IUploadSession> CreateUploadSession(ResolvedUploadJob resolvedUploadJob, CancellationToken cancellationToken = default)
        {
            
            var gs = CreateGraphClient(resolvedUploadJob);

            
            string? conflictBehavior = resolvedUploadJob.UploadTarget.FileExistsBehavior switch
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
            // var sharepointSite = resolvedUploadJob.UploadTargetConfig.ClientConfig?.SiteRelativePath ?? throw new ArgumentNullException(nameof(resolvedUploadJob.UploadTargetConfig.ClientConfig.SiteRelativePath));
            var sharepointSite = await gs.Sites.GetByPath(
                resolvedUploadJob.UploadTargetConfig.ClientConfig?.SiteRelativePath,
                resolvedUploadJob.UploadTargetConfig.ClientConfig?.SharepointHostname).Request().GetAsync(cancellationToken);
            
            if (sharepointSite == null)
                throw new ArgumentException(
                    "unable to locate sharepoint site with provided relativeSitePath and hostname");
            
            return await gs.Sites[sharepointSite.Id]
                .Drive.Root.ItemWithPath(itemPath)
                .CreateUploadSession(uploadProps)
                .Request()
                .PostAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        
    }
}