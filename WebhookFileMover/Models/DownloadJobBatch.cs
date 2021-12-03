using System;
using System.Collections.Generic;

namespace WebhookFileMover.Models
{
    public class DownloadJobBatch 
    {
        public DownloadJobBatch()
        {
        }

        public DownloadJobBatch(IEnumerable<DownloadJob>? jobs)
        {
            Jobs = jobs;
        }

        public IEnumerable<DownloadJob>? Jobs { get; set; }
        public int Id { get; set; }
        public string AssociatedDownloadConfigId { get; init; } = string.Empty;
        public IEnumerable<string> AssociatedUploadConfigIds { get; init; } = Array.Empty<string>();
        
        public int JobTrackingId { get; set; }
    }
}