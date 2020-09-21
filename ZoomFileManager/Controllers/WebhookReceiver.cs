using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZoomFileManager.Models;
using ZoomFileManager.Services;

namespace ZoomFileManager.Controllers
{
    [ApiController]
    [Route("api/zc")]
    
    public class WebhookReceiver : ControllerBase
    {
        private readonly ILogger<WebhookReceiver> _logger;
        private readonly RecordingManagementService _recordingManagementService;
        private readonly IOptions<WebhookRecieverOptions> _options;

        public WebhookReceiver(ILogger<WebhookReceiver> logger, RecordingManagementService recordingManagementService, IOptions<WebhookRecieverOptions> options)
        {
            this._logger = logger;
            this._recordingManagementService = recordingManagementService;
            this._options = options;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveNotification(ZoomWebhookEvent webhookEvent, [FromHeader(Name = "Authorization" )] string? authKey)
        {
            _logger.LogDebug("Received Webhook", HttpContext.Request);
            if (_options?.Value?.AllowedTokens?.Contains(authKey)?? false)
            {
                try
                {
                    await _recordingManagementService.DownloadFileAsync(webhookEvent).ConfigureAwait(false);
                    return NoContent();


                }
                catch (NullReferenceException ex)
                {
                    _logger.LogDebug("null ref", ex);
                    return new UnprocessableEntityResult();
                }
            }
            else
            {
                _logger.LogWarning($"invalid auth token received", HttpContext);
                return new ForbidResult();
            }


        }
    }
    public class WebhookRecieverOptions
    {
        public string[] AllowedTokens { get; set; } = Array.Empty<string>();
    }
}
