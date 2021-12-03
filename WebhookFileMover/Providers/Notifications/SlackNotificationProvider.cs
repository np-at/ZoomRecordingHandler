using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackAPI;
using WebhookFileMover.Helpers;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Notifications
{
    public class SlackNotificationProvider : INotificationProvider
    {
        private readonly ILogger<SlackNotificationProvider> _logger;
        private readonly TemplateResolverService _templateResolverService;
        private readonly SlackApiOptions _baseOptions;
        public SlackNotificationProvider(ILogger<SlackNotificationProvider> logger,
            TemplateResolverService templateResolverService, IOptions<SlackApiOptions> options)
        {
            _logger = logger;
            _templateResolverService = templateResolverService;
            _baseOptions = options.Value;
        }

        public async Task FireNotification(Notification notification, CancellationToken cancellationToken = default) =>
            await SendMessage(notification).ConfigureAwait(false);

        /// <summary>
        /// Attempts to find the slack user with associated email.  Returns a Slack User object if successful, else returns null
        /// </summary>
        /// <param name="userEmail">Email used to lookup user</param>
        /// <returns> <see cref="SlackAPI.User">Slack User Object Null if not found or error</see></returns>
        public async Task<User?> GetUserAsync(string userEmail, SlackApiOptions options)
        {
            var slackTaskClient = new SlackTaskClient(options.BotUserToken);
            var response = await slackTaskClient.GetUserByEmailAsync(userEmail).ConfigureAwait(false);
            return response.ok ? response.user : null;
        }

        /// <summary>
        /// Attempts to find the slack user id associated with the input email.
        /// </summary>
        /// <param name="userEmail">Email used to lookup user</param>
        /// <param name="options"></param>
        /// <returns>Slack Id as string.  Null if not found or error</returns>
        public async Task<string?> GetUserIdAsync(string userEmail)
        {
            
            var slackTaskClient = new SlackTaskClient(_baseOptions.BotUserToken);

            var response = await slackTaskClient.GetUserByEmailAsync(userEmail).ConfigureAwait(false);
            if (!response.ok)
                _logger.LogCritical("Error During Slack User ID fetch");
            return response.ok ? response.user.id : null;
        }

        public string? GetUserIdSync(string userEmail)
        {
            var slackTaskClient = new SlackTaskClient(_baseOptions.BotUserToken);
            var response = slackTaskClient.GetUserByEmailAsync(userEmail);
            response.Wait();
            
            return response.Result.ok ? response.Result.user.id : null;
        }

        /// <summary>
        /// Sends a message to the slack channel defined in SlackApiOptions in the startup Config object
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        private async Task SendMessage(Notification notification)
        {
            var opts = notification.NotificationProviderConfig?.SlackApiOptions ??
                       throw new Exception("Bot user token required in notification config; not found");

            var resolvedParamBag =
                await _templateResolverService.ResolveTemplateParams(notification.NotificationProviderConfig.ParamBag,
                    notification.WebEventStringBody,
                    ('L',
                        notification.Job?.JobTaskInstances?.First(x => !string.IsNullOrWhiteSpace(x.LocationUri))
                            .LocationUri ?? string.Empty));
            var formattedMessage =
                StringUtils.ApplyTemplatedFormattingString(
                    notification.NotificationProviderConfig.SuccessMessageTemplate ?? throw new InvalidOperationException(), resolvedParamBag);

            var slackTaskClient = new SlackTaskClient(opts.BotUserToken);

            try
            {
                // var res = new SlackClient(opts.BotUserToken);
                // res.PostMessage(Console.WriteLine, opts.Channel, formattedMessage);
                var response = await slackTaskClient.PostMessageAsync(opts.Channel ?? string.Empty, formattedMessage)
                    .ConfigureAwait(false);
                if (response.ok)
                    return;
                throw new HttpRequestException(response.error);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while posting Notification: {Notification}", formattedMessage);
                throw;
            }
        }
    }
}