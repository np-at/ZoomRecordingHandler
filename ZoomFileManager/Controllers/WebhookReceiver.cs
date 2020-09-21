using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZoomFileManager.Models;
using ZoomFileManager.Services;

namespace ZoomFileManager.Controllers
{
    [ApiController]
    [Route("zc")]
    
    public class WebhookReceiver : ControllerBase
    {
        private readonly ILogger<WebhookReceiver> _logger;
        private readonly RecordingManagementService _recordingManagementService;
        private readonly string _authorizationKey;

        public WebhookReceiver(ILogger<WebhookReceiver> logger, RecordingManagementService recordingManagementService)
        {
            this._logger = logger;
            this._recordingManagementService = recordingManagementService;
            this._authorizationKey = "poop";
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveNotification(ZoomWebhookEvent webhookEvent, [FromHeader(Name = "Authorization" )] string? authKey)
        {
            _logger.LogDebug("Received Webhook", HttpContext.Request);
            if (authKey?.Equals(_authorizationKey) ?? false)
            {
                await _recordingManagementService.DownloadFileAsync(webhookEvent).ConfigureAwait(false);
                return NoContent();
            }
            else
            {
                _logger.LogWarning($"invalid auth token received", HttpContext);
                return new ForbidResult();
            }


        }
    }
}
