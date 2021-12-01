using System;
using System.Collections.Generic;

namespace WebhookFileMover.Models
{
    public class DownloadJobBatch 
    {
        public IEnumerable<DownloadJob>? Jobs { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AssociatedDownloadConfigId { get; init; } = string.Empty;
        public IEnumerable<string> AssociatedUploadConfigIds { get; init; } = Array.Empty<string>();
    }
}