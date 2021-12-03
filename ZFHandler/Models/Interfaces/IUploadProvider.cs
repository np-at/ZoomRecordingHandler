using System.Threading;
using System.Threading.Tasks;

namespace ZFHandler.Models.Interfaces
{
    public interface IUploadProvider
    {
        Task PutFileAsync(CancellationToken cancellationToken = default);
        
        Task GetFileInfoAsync(CancellationToken cancellationToken = default);
        
        Task GetFileListAsync(CancellationToken cancellationToken = default);
        
        Task DeleteFileAsync(CancellationToken cancellationToken = default);
        
        Task UpdateFileAsync(CancellationToken cancellationToken = default);
    }

   
}