using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZoomFileManager.Services;

namespace ZoomFileManager.BackgroundServices
{
    public class ZoomEventProcessingService : BackgroundService
    {
        private readonly ILogger<ZoomEventProcessingService> _logger;
        private readonly ProcessingChannel _processingChannel;
        private readonly IServiceProvider _serviceProvider;

        public ZoomEventProcessingService(ILogger<ZoomEventProcessingService> logger, ProcessingChannel processingChannel, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _processingChannel = processingChannel;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach( var fileName in _processingChannel.ReadAllZoomEventsAsync(stoppingToken))
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<RecordingManagementService>();
                try
                {
                    await processor.DownloadFilesFromWebookAsync(fileName, stoppingToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        internal static class EventIds
        {
            
        }
    }
}