using System.Collections.Concurrent;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Models;

namespace ZFHandler.Services
{
    public static class JobTracker
    {
        internal static ConcurrentDictionary<string, DownloadJob> DownloadJobs { get; set; } = new();
        internal static ConcurrentDictionary<string, DownloadJobBatch> ConcurrentDictionary = new();
    }
}