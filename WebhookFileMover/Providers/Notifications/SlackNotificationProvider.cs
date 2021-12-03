using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackAPI;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Notifications
{
    public class SlackNotificationProvider : INotificationProvider
    {
        private readonly ILogger<SlackNotificationProvider> _logger;
        private readonly IOptionsSnapshot<SlackApiOptions> _slackOptionsSnapshot;
        
        public SlackNotificationProvider(ILogger<SlackNotificationProvider> logger, IOptionsSnapshot<SlackApiOptions> options)
        {
            
            _logger = logger;
            _slackOptionsSnapshot = options;
        }
        public async Task FireNotification(Notification notification, CancellationToken cancellationToken = default) => await SendMessage(notification).ConfigureAwait(false);

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
        /// <returns>Slack Id as string.  Null if not found or error</returns>
        public async Task<string?> GetUserIdAsync(string userEmail, SlackApiOptions options)
        {
            var slackTaskClient = new SlackTaskClient(options.BotUserToken);

            var response = await slackTaskClient.GetUserByEmailAsync(userEmail).ConfigureAwait(false);
            return response.ok ? response.user.id : null;
        }

        /// <summary>
        /// Sends a message to the slack channel defined in SlackApiOptions in the startup Config object
        /// </summary>
        /// <param name="message"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        private static async Task SendMessage(Notification notification)
        {
            var opts = notification.NotificationProviderConfig.SlackApiOptions ??
                        throw new Exception("Bot user token required in notification config; not found");
            
            var formattedMessage = notification.GenerateSuccessMessage();
            var slackTaskClient = new SlackTaskClient(opts.BotUserToken);
                
            var response = await slackTaskClient.PostMessageAsync(opts.Channel, formattedMessage).ConfigureAwait(false);
            if (response.ok)
                return;
            throw new HttpRequestException(response.error);
        }
    }
}