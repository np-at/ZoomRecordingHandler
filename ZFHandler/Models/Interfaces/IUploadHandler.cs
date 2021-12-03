using System.Threading;
using System.Threading.Tasks;

namespace ZFHandler.Models.Interfaces
{
    public interface IWebhookReceiverHandler<in T>
    {
        ValueTask<bool> ValidateWebhookAsync(T webhook, CancellationToken cancellationToken = default);
        
        bool ValidateWebhook(object webhook);
    }
    
}