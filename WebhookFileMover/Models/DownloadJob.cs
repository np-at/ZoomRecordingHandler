using System;
using System.IO;
using System.Net.Http;

namespace WebhookFileMover.Models
{
    public class DownloadJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string? ParentJobId { get; set; }

        public HttpRequestMessage? Message { get; set; }

        public FileInfo? DestinationFile { get; set; }

        public string? DestinationFolderPath { get; set; }

        public string? DestinationFileName { get; set; }

        public CompletedDownloadJob CompleteDownloadJob(FileInfo downloadedFileInfo) => new(this, downloadedFileInfo);
    }
}