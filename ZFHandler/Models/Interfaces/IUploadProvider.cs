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

    public abstract class UploadProvider<T> : IUploadProvider where T : class
    {
        protected UploadProvider()
        {
            
        }
        public async Task PutFileAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task GetFileInfoAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task GetFileListAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeleteFileAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task UpdateFileAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}