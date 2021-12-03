using System.Threading;
using System.Threading.Tasks;

namespace ZoomFileManager.Services
{
    public interface INotificationProvider
    {
        public Task SendNotificationAsync(CancellationToken cancellationToken = default);

    }
}