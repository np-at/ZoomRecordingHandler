using System;
using System.IO;
using MediatR;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Models
{
    public struct UploadJobSpec<T> : INotification
    {
        public string TargetDir;
        
        public string TargetName;

        public FileInfo FileInfo; 

        public JobType JobType;

        public UploadTarget[] UploadTargets;

        public Batch BatchInfo;
        public JobStatus Status { get; set; }
        public string Id { get; set; }

        public ArraySegment<string> UploadTargetIds { get; set; }
    }
}