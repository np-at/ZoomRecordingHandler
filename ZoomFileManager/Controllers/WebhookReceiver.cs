using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZoomFileManager.Models;
using ZoomFileManager.Services;

namespace ZoomFileManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookReceiver : ControllerBase
    {
        private readonly ILogger<WebhookReceiver> _logger;
        private readonly RecordingManagementService recordingManagementService;
        private readonly string authorizationKey;

        public WebhookReceiver(ILogger<WebhookReceiver> logger, RecordingManagementService recordingManagementService,string authorizationKey)
        {
            this._logger = logger;
            this.recordingManagementService = recordingManagementService;
            this.authorizationKey = authorizationKey;
        }

        [HttpPost]
        [RequireHttps]
        public async Task<IActionResult> ReceiveNotification(ZoomWebhookEvent webhookEvent, [FromHeader(Name = "authorization" )] string authKey)
        {
            _logger.LogDebug("Received Webhook", HttpContext.Request);
            if (authKey.Equals(authorizationKey))
            {
                await recordingManagementService.DownloadFileAsync(webhookEvent).ConfigureAwait(false);
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
