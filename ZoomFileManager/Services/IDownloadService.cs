using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Models.ConfigurationSchemas;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public interface IValidator<in T>
    {
        public ValueTask<bool> ValidateWebhookParametersAsync(T webhookEvent);

    }

    
    public interface IDownloadService<in T> : IValidator<T>
    {
        // public Task DownloadFromWebhookAsync(T webhookEvent, IHttpClientFactory httpClientFactory, CancellationToken ct = default);
        public Task<IEnumerable<DownloadJob>> GenerateDownloadJobsFromWebhookAsync(T webhookEvent, CancellationToken ct = default);
    }

    public class DownloadService : IDownloadService<Zoominput>
    {
        private readonly ILogger<DownloadService> _logger;
        
        public DownloadService(ILogger<DownloadService> logger, IOptions<AppConfig> options)
        {
            _logger = logger;
        }

        public ValueTask<bool> ValidateWebhookParametersAsync(Zoominput webhookEvent)
        {
            return new ValueTask<bool>(true);
        }

        public async Task DownloadFromWebhookAsync(Zoominput webhookEvent, IHttpClientFactory httpClientFactory,
            CancellationToken ct = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<DownloadJob>> GenerateDownloadJobsFromWebhookAsync(Zoominput webhookEvent, CancellationToken ct = default)
        {
            throw new System.NotImplementedException();
        }
    }
}