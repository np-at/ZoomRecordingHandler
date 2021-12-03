using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Notifications
{
    public class WebhookNotificationProviderOptions
    {
        public HttpMethod HttpMethod { get; init; }
        public Uri UrlEndpoint { get; init; }
        public HttpRequestHeaders HttpRequestHeaders { get; init; }
        public string MessageTemplate { get; init; }
        
    }
    public class WebhookNotificationProvider : INotificationProvider
    {
        private readonly ILogger<WebhookNotificationProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<WebhookNotificationProviderOptions> _optionsMonitor;

        public WebhookNotificationProvider(ILogger<WebhookNotificationProvider> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<WebhookNotificationProviderOptions> optionsMonitor)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _optionsMonitor = optionsMonitor;
        }

        private async Task SendWebhookNotification(string endpoint, string message)
         {
             string? jsonMessage = $"{{\"text\": \"{message}\"}}";
             using var client = _httpClientFactory.CreateClient();
             var responseMessage = await client.PostAsync(endpoint,
                 new StringContent(jsonMessage, Encoding.UTF8, "application/json")).ConfigureAwait(false);
             if (!responseMessage.IsSuccessStatusCode)
                 _logger.LogError(
                     "Unsuccessful in activating notification provider at endpoint: {Endpoint} \n for message: \n {Message}", endpoint, message);
         }
        public async Task FireNotification(Notification notification, CancellationToken cancellationToken = default)
        {
            var formatType = string.Empty;
             using var httpClient = _httpClientFactory.CreateClient();
             var config = notification.NotificationProviderConfig.ProviderType switch
             {
                 NotificationProviderType.Unknown => throw new Exception(),
                 NotificationProviderType.SlackBot => new WebhookNotificationProviderOptions(),
                 _ => throw new ArgumentOutOfRangeException()
             };
             // }
             // // var config = _optionsMonitor.Get(notification.NotificationConfigId) ?? throw new KeyNotFoundException($"Unable to find WebhookNotificationProviderOptions instance for provided Config ID of {notification.NotificationConfigId}");
             // var message = new HttpRequestMessage(config.HttpMethod, config.UrlEndpoint);
             //
             // var contentTypeHeaders = config.HttpRequestHeaders.GetValues("Content-Type").ToImmutableArray();
             // // figure out how we need to format the content
             // if (contentTypeHeaders.Any())
             //     formatType = contentTypeHeaders[0] switch
             //     {
             //         "application/json" => "application/json",
             //         _ => throw new NotImplementedException()
             //     };




        }
    }
}