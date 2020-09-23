using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            _logger = logger;
            _options = options;
        }
      
        private async Task HandleApiFallback(HttpContext context)
        {
            context.Request.EnableBuffering();
   
            var tempPath = Path.Join(Path.GetTempPath(), "asdf");
             
             try
             {
                 await using  var buff = new MemoryStream();
                 context.Request.Body.Position = 0;

                 await context.Request.Body.CopyToAsync(buff);
             
             
                 
                 await using var s = System.IO.File.Create(tempPath);
                 await using var wrt = new StreamWriter(s);
                 byte[] result = new byte[buff.Length];
                 buff.Position = 0;
                 using var rdr = new StreamReader(buff);
                 var sti = await rdr.ReadToEndAsync();
                 var str = System.Text.Encoding.UTF8.GetString(result);
                 await wrt.WriteAsync(str);
                 Console.WriteLine(sti);
                 _logger.LogWarning($"request contents dumped to {tempPath}");
                 // _logger.LogWarning($"{(await System.IO.File.OpenText(tempPath).ReadToEndAsync())}");
             }
             catch (Exception e)
             {
                 Console.WriteLine(e);
                 throw;
             }
             
             context.Response.StatusCode = StatusCodes.Status404NotFound;
         }

        [HttpPost]
        public IActionResult ReceiveNotification([FromBody] ZoomWebhookEvent webhookEvent, [FromServices]
            IServiceScopeFactory
                serviceScopeFactory, [FromHeader(Name = "Authorization")] string? authKey)
        {
            // if (!ModelState.IsValid || webhookEvent.Event == null || !webhookEvent.Event.Any())
            //     HandleApiFallback(HttpContext);

            string? remoteHost = HttpContext.Request.Host.ToString();
            _logger.LogDebug($"Received Webhook from ${remoteHost} {0}");

            if (!string.IsNullOrWhiteSpace(authKey) && (_options?.Value?.AllowedTokens?.Contains(authKey) ?? false))
            {
                try
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {

                            using var scope = serviceScopeFactory.CreateScope();
                            using var recService = scope.ServiceProvider.GetRequiredService<RecordingManagementService>();
                            await recService.DownloadFilesFromWebookAsync(webhookEvent).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                            
                        }
                    });

                    return NoContent();
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogDebug("null ref", ex);
                    return new UnprocessableEntityResult();
                }
            }

            _logger.LogWarning($"invalid auth token received from ${remoteHost}");
            return new ForbidResult();
        }
    }

    public class WebhookRecieverOptions
    {
        public string[] AllowedTokens { get; set; } = Array.Empty<string>();
    }
}