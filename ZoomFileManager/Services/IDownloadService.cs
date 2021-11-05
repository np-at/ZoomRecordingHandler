using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZoomFileManager.Models;
using ZoomFileManager.Models.ConfigurationSchemas;

namespace ZoomFileManager.Services
{
    public interface IDownloadService<T>
    {
        public Task<bool> ValidateWebhookParametersAsync(T webhookEvent, CancellationToken ct = default);
        public Task DownloadFromWebhookAsync(T webhookEvent, CancellationToken ct = default);
    }

    public class DownloadService : IDownloadService<ZoomWebhookEvent>
    {
        private readonly ILogger<DownloadService> _logger;
        
        public DownloadService(ILogger<DownloadService> logger, IOptions<AppConfig> options)
        {
            _logger = logger;
        }

        public async Task<bool> ValidateWebhookParametersAsync(ZoomWebhookEvent webhookEvent, CancellationToken ct = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task DownloadFromWebhookAsync(ZoomWebhookEvent webhookEvent, CancellationToken ct = default)
        {
            throw new System.NotImplementedException();
        }
    }
}