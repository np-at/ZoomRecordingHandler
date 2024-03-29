﻿using System.IO;

namespace WebhookFileMover.Models
{
    public class CompletedDownloadJob
    {
        public int DownloadJobId { get; set; }
        
        public FileInfo DownloadedFile { get; set; }
        
        public CompletedDownloadJob(DownloadJob parentDownloadJob, FileInfo downloadedFileInfo)
        {
            DownloadJobId = parentDownloadJob.Id;
            DownloadedFile = downloadedFileInfo;
        }
    }
}