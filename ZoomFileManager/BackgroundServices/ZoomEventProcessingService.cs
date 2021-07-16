using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZoomFileManager.Models;
using ZoomFileManager.Services;

namespace ZoomFileManager.BackgroundServices
{
    public class ZoomEventProcessingService : BackgroundService
    {
        private readonly ILogger<ZoomEventProcessingService> _logger;
        private readonly PChannel<ZoomWebhookEvent> _processingChannel;
        private readonly IServiceProvider _serviceProvider;

        public ZoomEventProcessingService(ILogger<ZoomEventProcessingService> logger,
            PChannel<ZoomWebhookEvent> processingChannel, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _processingChannel = processingChannel;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<RecordingManagementService>();

                    var webhookEvent = await _processingChannel.ReadChannelEventAsync(stoppingToken);
                    try
                    {

                        await processor.DownloadFilesFromWebhookAsync(webhookEvent, stoppingToken);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e}");
                        await _processingChannel.AddEventAsync(webhookEvent, stoppingToken);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation Cancelled exception occured");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured");
                _processingChannel.TryCompleteWriter(ex);
            }
            finally
            {
                _processingChannel.TryCompleteWriter();
            }
        }

        internal static class EventIds
        {
        }
    }
}