using System.Threading;
using System.Threading.Tasks;

namespace ZFHandler.Models.Interfaces
{
    public interface INotificationProvider
    {
        Task SendNotificationAsync(string notificationText, CancellationToken cancellationToken = default);
        
    }
}