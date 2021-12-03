using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Helpers;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Notifications
{
    public class WebhookNotificationProviderOptions
    {
        public HttpMethod? HttpMethod { get; init; }
        public Uri? UrlEndpoint { get; init; }
        public HttpRequestHeaders? HttpRequestHeaders { get; init; }
        public string? MessageTemplate { get; init; }
    }

    public class WebhookNotificationProvider : INotificationProvider
    {
        private readonly ILogger<WebhookNotificationProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<WebhookNotificationProviderOptions> _optionsMonitor;
        private readonly TemplateResolverService _templateResolverService;

        public WebhookNotificationProvider(ILogger<WebhookNotificationProvider> logger,
            IHttpClientFactory httpClientFactory, IOptionsMonitor<WebhookNotificationProviderOptions> optionsMonitor, TemplateResolverService templateResolverService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _optionsMonitor = optionsMonitor;
            _templateResolverService = templateResolverService;
        }

        private async Task SendWebhookNotification(string endpoint, string jsonMessage)
        {
            // string? jsonMessage = $"{{\"text\": \"{message}\"}}";

            using var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.PostAsync(endpoint,
                new StringContent(jsonMessage, Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!responseMessage.IsSuccessStatusCode)
                _logger.LogError(
                    "Unsuccessful in activating notification provider at endpoint: {Endpoint} \n for message: \n {Message}",
                    endpoint, jsonMessage);
        }

        public async Task FireNotification(Notification notification, CancellationToken cancellationToken = default)
        {
            var bodyTemplate = notification.NotificationProviderConfig?.WebhookOptions?.BodyTemplate ??
                               throw new InvalidOperationException();

            var itemLink = (notification.Job?.JobTaskInstances?.First(x => !string.IsNullOrWhiteSpace(x.LocationUri))
                .LocationUri ?? string.Empty).TrimEnd('/');
            var resolvedParamBag =
                await _templateResolverService.ResolveTemplateParams(notification.NotificationProviderConfig.ParamBag,
                    notification.WebEventStringBody,
                    ('L', itemLink.Remove(itemLink.LastIndexOf('/'))));
            var formattedMessage =
                StringUtils.ApplyTemplatedFormattingString(
                    notification.NotificationProviderConfig.SuccessMessageTemplate ?? throw new InvalidOperationException(), resolvedParamBag);

            var jsonString = StringUtils.ApplyTemplatedFormattingString(bodyTemplate,
                new[] { ('S', formattedMessage) });

            using var httpClient = _httpClientFactory.CreateClient();
            var endpoints = notification.NotificationProviderConfig?.WebhookOptions?.Endpoints ??
                            throw new Exception("No Webhook Endpoints specified");
            foreach (string webhookOptionsEndpoint in endpoints)
            {
                List<Exception> exceptions = new();
                try
                {
                    var httpcontent = new StringContent(jsonString);
                    httpcontent.Headers.ContentType =
                        MediaTypeHeaderValue.Parse("application/json"); // .Add("Content-Type", "application/json");
                    var response = await httpClient.PostAsync(webhookOptionsEndpoint, httpcontent,
                        cancellationToken);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError(e, "Error while sending webhook notification");
                    exceptions.Add(e);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error");
                    exceptions.Add(e);
                }

                if (exceptions.Any())
                    throw new AggregateException(exceptions);
            }
            // var config = notification.NotificationProviderConfig?.ProviderType switch
            // {
            //     NotificationProviderType.Unknown => throw new Exception(),
            //     NotificationProviderType.SlackBot => new WebhookNotificationProviderOptions(),
            //     _ => throw new ArgumentOutOfRangeException()
            // };
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