using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Models.Interfaces
{
    public interface INotificationProvider
    {
        Task FireNotification(Notification notification, CancellationToken cancellationToken = default);
    }
}