using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Helpers;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Models.Configurations.Internal
{
    public record ResolvedUploadTarget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public UploadTarget? UploadTarget { get; set; }
        public UploadTargetConfig? UploadTargetConfig { get; set; }

        public IEnumerable<NotificationProviderConfig> NotificationProviderConfig { get; set; } = Array.Empty<NotificationProviderConfig>();

        public ResolvedUploadJob CreateUploadJob(CompletedDownloadJob completedDownloadJob)
        {
            if (completedDownloadJob.DownloadJobId == 0)
                throw new NullReferenceException(
                    "Download job does not have assigned Job Tracker id (DownloadJobId found to be 0)");
            var job = new ResolvedUploadJob(completedDownloadJob.DownloadedFile)
            {
                Id = Id,
                UploadTarget = UploadTarget,
                UploadTargetConfig = UploadTargetConfig,
                AssociatedJobTaskInstanceId = completedDownloadJob.DownloadJobId,
                NotificationProviderConfig = this.NotificationProviderConfig
            };
            return job;
        }
    }

    public record ResolvedUploadJob : ResolvedUploadTarget
    {
        /// <summary>
        /// Corresponds to the id of the associated <see cref="JobTaskInstance.Id"/> in the tracking db
        /// </summary>
        public int AssociatedJobTaskInstanceId { get; set; }

        public ResolvedUploadJob(FileInfo sourceFile)
        {
            SourceFile = sourceFile;
        }

        public FileInfo SourceFile { get; }

        public string GetRelativePath()
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append('/');
            strBuilder.Append(UploadTargetConfig.RootPath?.Trim('/'));
            strBuilder.Append('/');
            if (UploadTarget.RelativeRootUploadPath != null)
            {
                strBuilder.Append(this.UploadTarget.RelativeRootUploadPath?.Trim('/'));
                strBuilder.Append('/');
            }

            var templateArr = StringUtils.DefaultTemplateVals.Append(('N',
                SourceFile.Directory?.Name ?? "Directory")).ToArray();
            var dirName = StringUtils.ApplyTemplatedFormattingString(
                UploadTarget.DirectoryNamingTemplate ?? throw new InvalidOperationException(), templateArr).Trim('/');
            strBuilder.Append(dirName);


            strBuilder.Append('/');

            string? fileName =
                StringUtils.ApplyTemplatedFormattingString(
                    UploadTarget.NamingTemplate ?? throw new ArgumentNullException(),
                    StringUtils.DefaultTemplateVals
                        .Append(('N', SourceFile.Name.TrimEnd(SourceFile.Extension.ToCharArray())))
                        .Append(('E', SourceFile.Extension.TrimStart('.'))).ToArray()).Trim('/');
            strBuilder.Append(fileName);


            return strBuilder.ToString();
        }
    }
}