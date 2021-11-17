using System.Threading;
using System.Threading.Tasks;
using ZFHandler.Models.Interfaces;

namespace ZFHandler.Services.BaseProviderImplementations.NotificationServices
{
    public class SlackNotificationProvider : INotificationProvider
    {
        
        public async Task SendNotificationAsync(string notificationText, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}