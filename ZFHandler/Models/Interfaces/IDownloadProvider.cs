using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZFHandler.Models.Interfaces
{
    public interface IDownloadProvider
    {
        Task<FileInfo> DownloadFileAsync(Uri uri, CancellationToken cancellationToken = default);
    }
}