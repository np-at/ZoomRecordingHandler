// using System.Threading;
// using System.Threading.Tasks;
// using MediatR;
// using Microsoft.Extensions.FileProviders;
// using Microsoft.Graph;
// using ZFHandler.Models;
// using ZoomFileManager.Models;
//
// namespace ZoomFileManager.Services
// {
//     public interface IUploadService : IRequestHandler<UploadJobSpec, bool>
//     {
//         Task GetFileAsync(CancellationToken cancellationToken = default);
//         Task GetFilesAsync(CancellationToken cancellationToken = default);
//         Task DeleteFileAsync(CancellationToken cancellationToken = default);
//         Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo sourceFileInfo, string? relativePath, CancellationToken cancellationToken = default);
//
//         Task ProcessUploadJobAsync(UploadJobSpec uploadJobSpec, CancellationToken cancellationToken = default);
//
//     }
// }