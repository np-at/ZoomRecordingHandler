using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private async Task HandleApiFallback(HttpContext context)
        {

            try
            {

                await using var requestBody = context.Request?.Body ?? throw new HttpRequestException();

                string tempPath = Path.GetTempPath() + Guid.NewGuid().ToString();
                await using var s = System.IO.File.Create(tempPath);
                await requestBody.CopyToAsync(s);
                 _logger.LogWarning($"request contents dumped to {tempPath}");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
        [HttpPost]
        public IActionResult ReceiveNotification([FromBody] ZoomWebhookEvent webhookEvent, [FromServices] IServiceScopeFactory
                                    serviceScopeFactory, [FromHeader(Name = "Authorization")] string? authKey)
        {
            if (!ModelState.IsValid)
            {
                _ = Task.Run(async () => { await HandleApiFallback(HttpContext); });
            }
            

            string? remoteHost = HttpContext.Request.Host.ToString();
            _logger.LogDebug($"Received Webhook from ${remoteHost}");
            
            if (!string.IsNullOrWhiteSpace(authKey) && (_options?.Value?.AllowedTokens?.Contains(authKey) ?? false))
            {
                try
                {
                    _ = Task.Run(async () =>
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        using var recService = scope.ServiceProvider.GetRequiredService<RecordingManagementService>();
                        await recService.DownloadFilesFromWebookAsync(webhookEvent).ConfigureAwait(false);
                     
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
