using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Graph;

namespace ZoomFileManager.Services
{
    public class UploadService<T> : IUploadService
    {
        public UploadService()
        {
            
        }
        public async Task GetFilesAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(string uploadTarget, IFileInfo filePath, string? relativePath)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeleteFileAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo sourceFileInfo, string? relativePath, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo fileInfo, string? relativePath)
        {
            throw new System.NotImplementedException();
        }
    }
}