using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace ZFHandler.Models
{
    public class Batch
    {
        /// <summary>
        /// Path relative to file provider where files associated with this batch will be handled
        /// </summary>
        public string BasePath { get; }

        public string? Origin;

        public IEnumerable<JobFile>? Files;

        public Batch(string? basePath = null)
        {
            BasePath = basePath ?? Guid.NewGuid().ToString("N");
        }
    }

    public class JobFile
    {
        public IFileInfo? FileInfo;

        public FileType FileType;

        public FileStatus FileStatus;

    }

    public enum FileStatus
    {
        Unknown,
        Absent,
        Downloading,
        Downloaded,
        Uploading,
        NeedsReprocessing
    }
    public enum FileType
    {
        Unknown,
        Audio,
        Video,
        Subtitle,
        Document
        
    }
}