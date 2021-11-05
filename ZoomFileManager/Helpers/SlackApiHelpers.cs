using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackAPI;

namespace ZoomFileManager.Helpers
{
    public interface ISlackApiOperations
    {
        Task GetMessageAsync(string message);
        
        
    }
    public class SlackApiOptions
    {
        public string? BotUserToken { get; set; }
        public string? Channel { get; set; }
        
    }
    public class SlackApiHelpers
    {
        private readonly ILogger<SlackApiHelpers> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SlackApiOptions _options;
        private readonly SlackTaskClient _slackTaskClient;

        public SlackApiHelpers(ILogger<SlackApiHelpers> logger, IOptions<SlackApiOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options.Value ?? new SlackApiOptions();
            _slackTaskClient = new SlackTaskClient(_options.BotUserToken);
        }

        /// <summary>
        /// Attempts to find the slack user with associated email.  Returns a Slack User object if successful, else returns null
        /// </summary>
        /// <param name="userEmail">Email used to lookup user</param>
        /// <returns> <see cref="SlackAPI.User">Slack User Object Null if not found or error</see></returns>
        public async Task<User?> GetUserAsync(string userEmail)
        {
            var response = await _slackTaskClient.GetUserByEmailAsync(userEmail).ConfigureAwait(false);
            return response.ok ? response.user : null;
        }

        /// <summary>
        /// Attempts to find the slack user id associated with the input email.
        /// </summary>
        /// <param name="userEmail">Email used to lookup user</param>
        /// <returns>Slack Id as string.  Null if not found or error</returns>
        public async Task<string?> GetUserIdAsync(string userEmail)
        {
            var response = await _slackTaskClient.GetUserByEmailAsync(userEmail).ConfigureAwait(false);
            return response.ok ? response.user.id : null;
        }
        
        /// <summary>
        /// Sends a message to the slack channel defined in SlackApiOptions in the startup Config object
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {

            var response = await _slackTaskClient.PostMessageAsync(_options.Channel, message).ConfigureAwait(false);
            if (response.ok)
                return;
            throw new HttpRequestException(response.error);
        }
        
    }
}