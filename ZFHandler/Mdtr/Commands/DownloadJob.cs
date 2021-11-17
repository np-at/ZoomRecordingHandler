using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using MediatR;

namespace ZFHandler.Mdtr.Commands
{
    public class DownloadJobBatch : INotification
    {
        public IEnumerable<DownloadJob>? Jobs { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
    }
    public class DownloadJob : IRequest<FileInfo?>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string? ParentJobId { get; set; }

        public JobStatus Status { get; set; } = JobStatus.Null;
        public HttpRequestMessage message { get; set; }
        
        public FileInfo? DestinationFile { get; set; }
        
        public string DestinationFolderPath { get; set; }
        
        public string DestinationFileName { get; set; }
        
    }

    public enum JobStatus
    {
        Null,
        NotStarted,
        InProgress,
        Failed,
        Completed
    }
}