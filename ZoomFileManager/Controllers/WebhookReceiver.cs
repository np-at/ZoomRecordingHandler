using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZoomFileManager.Models;
using ZoomFileManager.Services;

namespace ZoomFileManager.Controllers
{
    // [ApiController]
    // [Route("api/zc")]
    public class WebhookReceiver : ControllerBase
    {
        private readonly ILogger<WebhookReceiver> _logger;
        private readonly IOptions<WebhookReceiversOptions> _options;
        // private readonly ProcessingChannel _processingChannel;
        private readonly IMediator _mediator;
        private readonly IDownloadService<Zoominput> _downloadService;
        public WebhookReceiver(ILogger<WebhookReceiver> logger, IOptions<WebhookReceiversOptions> options, IMediator mediator, IDownloadService<Zoominput> downloadService)
        {
            _logger = logger;
            _options = options;
            // _processingChannel = processingChannel;
            _mediator = mediator;
            _downloadService = downloadService;
        }


        private async Task HandleApiFallback(HttpContext context)
        {
            context.Request.EnableBuffering();

            string? tempPath = Path.Join(Path.GetTempPath(), "TransformFunction");

            try
            {
                await using var buff = new MemoryStream();
                context.Request.Body.Position = 0;

                await context.Request.Body.CopyToAsync(buff).ConfigureAwait(false);


                await using var s = System.IO.File.Create(tempPath);
                await using var wrt = new StreamWriter(s);
                byte[] result = new byte[buff.Length];
                buff.Position = 0;
                using var rdr = new StreamReader(buff);
                string? sti = await rdr.ReadToEndAsync().ConfigureAwait(false);
                string? str = Encoding.UTF8.GetString(result);
                await wrt.WriteAsync(str).ConfigureAwait(false);
                Console.WriteLine(sti);
                _logger.LogWarning("request contents dumped to {TempPath}", tempPath);
                // _logger.LogWarning($"{(await System.IO.File.OpenText(tempPath).ReadToEndAsync())}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }

//         [HttpPost("v3")]
//         public async Task<IActionResult> ReceiveNotificationV3([FromBody] Zoominput webhookEvent,
//             [FromHeader(Name = "Authorization")] string? authKey,
//             CancellationToken cancellationToken)
//         {
// #if DEBUG
//             string? jsonString = JsonSerializer.Serialize(webhookEvent);
//             string? remoteHost = HttpContext.Request.Host.ToString();
//             _logger.LogInformation("Received Webhook from {RemoteHost} {JsonString}", remoteHost, jsonString);
// #endif
//
//             if (!string.IsNullOrWhiteSpace(authKey) && (_options?.Value?.AllowedTokens?.Contains(authKey) ?? false))
//             {
//                 try
//                 {
//                     if (await _processingChannel.AddZoomEventAsync(webhookEvent, cancellationToken)
//                         .ConfigureAwait(false))
//                     {
//                         return new NoContentResult();
//                     }
//
//                     return new StatusCodeResult(503);
//                 }
//                 catch (NullReferenceException ex)
//                 {
//                     _logger.LogDebug("null ref: {Error}", ex.Message);
//                     return new UnprocessableEntityResult();
//                 }
//             }
//
//             _logger.LogWarning("invalid auth token received {RemoteHost}", remoteHost);
//             return new ForbidResult();
//         }

        [HttpPost]
        public async Task<IActionResult> ReceiveNotificationV2([FromBody] Zoominput webhookEvent,
            CancellationToken cancellationToken)
        {
            try
            {
                var dlJob = await _downloadService.GenerateDownloadJobsFromWebhookAsync(webhookEvent, cancellationToken);
                foreach (var downloadJob in dlJob)
                {
                    await _mediator.Publish(downloadJob, cancellationToken).ConfigureAwait(false);
                }

                return new AcceptedResult();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
                
            // await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            // var result = await _mediator.Send();
            // var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // cts.CancelAfter(TimeSpan.FromSeconds(3));
            // if (ModelState.IsValid)
            // {
            //     try
            //     {
            //         var eventAdded = await _processingChannel.AddFileAsync(webhookEvent.DownloadToken, cts.Token);
            //         if (eventAdded)
            //         {
            //             return new OkResult();
            //         }
            //     }
            //     catch (OperationCanceledException) when (cts.IsCancellationRequested)
            //     {
            //       // ignore
            //     }
            // }
            return new BadRequestResult();
        }

        // [HttpPost]
        // public async Task<IActionResult> ReceiveNotification([FromBody] Zoominput webhookEvent,
        //     [FromHeader(Name = "Authorization")] string? authKey, CancellationToken cancellationToken)
        // {
        //     // if (!ModelState.IsValid || webhookEvent.Event == null || !webhookEvent.Event.Any())
        //     //     HandleApiFallback(HttpContext);
        //     string? jsonString = JsonSerializer.Serialize(webhookEvent);
        //     string? remoteHost = HttpContext.Request.Host.ToString();
        //     _logger.LogInformation("Received Webhook from {RemoteHost} {JsonString}", remoteHost, jsonString);
        //     if (!string.IsNullOrWhiteSpace(authKey) && (_options?.Value?.AllowedTokens?.Contains(authKey) ?? false))
        //     {
        //         try
        //         {
        //             if (await _processingChannel.AddZoomEventAsync(webhookEvent, cancellationToken)
        //                 .ConfigureAwait(false))
        //             {
        //                 return new NoContentResult();
        //             }
        //
        //             return new StatusCodeResult(503);
        //         }
        //         catch (NullReferenceException ex)
        //         {
        //             _logger.LogDebug("null ref: {Error}", ex.Message);
        //             return new UnprocessableEntityResult();
        //         }
        //     }
        //
        //     _logger.LogWarning("invalid auth token received {RemoteHost}", remoteHost);
        //     return new ForbidResult();
        // }
    }

    public class WebhookReceiversOptions
    {
        public string[]? AllowedTokens { get; set; } = Array.Empty<string>();
    }
}