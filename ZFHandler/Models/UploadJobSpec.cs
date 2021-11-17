using System;
using System.IO;
using MediatR;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Models.ConfigurationSchemas;

namespace ZFHandler.Models
{
    public struct UploadJobSpec<T> : INotification where T : BaseClientConfig
    {
        public string TargetDir;
        
        public string TargetName;

        public FileInfo FileInfo; 

        public JobType JobType;

        public UploadTarget[] UploadTargets;

        public Batch BatchInfo;
        public JobStatus Status { get; set; }
        public string Id { get; set; }


    }
}