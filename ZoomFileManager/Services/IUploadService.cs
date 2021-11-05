using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Graph;

namespace ZoomFileManager.Services
{
    public interface IUploadService
    {
        Task GetFilesAsync();
        Task DeleteFileAsync();
        Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo sourceFileInfo, string? relativePath, CancellationToken cancellationToken = default);
        
        
        
    }
}