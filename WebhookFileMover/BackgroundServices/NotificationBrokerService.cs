using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Channels;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;
using WebhookFileMover.Providers.Notifications;

namespace WebhookFileMover.BackgroundServices
{
    public class NotificationBrokerService : BackgroundService
    {
        private readonly ILogger<NotificationBrokerService> _logger;
        private readonly JobQueueChannel _jobQueueChannel;
        private readonly IServiceProvider _serviceProvider;
        
        public NotificationBrokerService(ILogger<NotificationBrokerService> logger, JobQueueChannel jobQueueChannel, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _jobQueueChannel = jobQueueChannel;
            _serviceProvider = serviceProvider;
        }

        private async Task HandleNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
             using var scope = _serviceProvider.CreateScope();
             INotificationProvider process = notification.NotificationProviderConfig?.ProviderType switch
             {
                 NotificationProviderType.SlackBot => scope.ServiceProvider.GetRequiredService<SlackNotificationProvider>(),
                 NotificationProviderType.Webhook => scope.ServiceProvider.GetRequiredService<WebhookNotificationProvider>(),
                 NotificationProviderType.Unknown => throw new Exception("Notification Provider Type not Specified"),
                 _ => throw new ArgumentOutOfRangeException()
             };
             await process.FireNotification(notification, cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var notification = await _jobQueueChannel.ReadNotificationAsync(stoppingToken);
                    await HandleNotificationAsync(notification, stoppingToken);

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing from Notification broker service");
                    throw;
                }
            }
        }
    }
}