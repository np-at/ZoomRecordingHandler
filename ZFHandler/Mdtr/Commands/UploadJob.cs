using MediatR;
using Microsoft.Extensions.FileProviders;

namespace ZFHandler.Mdtr.Commands
{
    public class UploadJob<T> : INotification
    {
        public IFileInfo? FileInfo { get; set; }
        
        public T UploadType { get; set; }
        
    }
}