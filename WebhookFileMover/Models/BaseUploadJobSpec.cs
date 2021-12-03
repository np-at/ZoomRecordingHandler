using System.Collections.Generic;
using System.IO;

namespace WebhookFileMover.Models
{
    public abstract class BaseUploadJobSpec
    {
        public string? TargetDir;
        public string? TargetName;
        public FileInfo? FileInfo;
        public ICollection<string>? UploadTargetIds { get; init; }
    }
}