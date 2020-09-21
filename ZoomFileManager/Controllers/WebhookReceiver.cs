using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IOptions<WebhookRecieverOptions> _options;

        public WebhookReceiver(ILogger<WebhookReceiver> logger, IOptions<WebhookRecieverOptions> options)
        {
            this._logger = logger;
            this._options = options;
        }

        [HttpPost]
        public IActionResult ReceiveNotification([FromBody] ZoomWebhookEvent webhookEvent, [FromServices] IServiceScopeFactory
                                    serviceScopeFactory, [FromHeader(Name = "Authorization")] string? authKey)
        {
            string? remoteHost = HttpContext.Request.Host.ToString();
            _logger.LogDebug($"Received Webhook from ${remoteHost}");
            if (_options?.Value?.AllowedTokens?.Contains(authKey) ?? false)
            {
                try
                {
                    _ = Task.Run(async () =>
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        using var recService = scope.ServiceProvider.GetRequiredService<RecordingManagementService>();
                        await recService.DownloadFileAsync(webhookEvent).ConfigureAwait(false);
                     
                    });
              
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
                _logger.LogWarning($"invalid auth token received from ${remoteHost}");
                return new ForbidResult();
            }


        }
    }
    public class WebhookRecieverOptions
    {
        public string[] AllowedTokens { get; set; } = Array.Empty<string>();
    }
}
