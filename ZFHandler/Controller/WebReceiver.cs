using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Pipelines.TPL;
using ZFHandler.Mdtr.Handlers;

namespace ZFHandler.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [GeneratedController]
    public class WebReceiver<T> : ControllerBase where T : class, IRConv<T>, new()
    {
        private readonly ILogger<WebReceiver<T>> _logger;
        private readonly IMediator _mediator;
        private readonly TPLFlowImplementation<T> _implementation;
        private readonly ReceiverTransformHandler<T> _transformHandler;

        public WebReceiver(ILogger<WebReceiver<T>> logger, IMediator mediator, TPLFlowImplementation<T> implementation, ReceiverTransformHandler<T> transformHandler)
        {
            _logger = logger;
            _mediator = mediator;
            _implementation = implementation;
            _transformHandler = transformHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] T webhookEvent, CancellationToken ct = default)
        {
            try
            {
                // _implementation.AddWebhookReceiver(_transformHandler.Handle2);
                // _implementation.AddDownloadStep<DefaultDownloadHandler>();
                _implementation.Finished += ImplementationOnFinished;
                await _implementation.RunAsync(webhookEvent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new BadRequestResult();
                throw;
            }
            finally
            {
                _implementation.Finished -= ImplementationOnFinished;
            }

            return new AcceptedResult();


//             try
//             {
//                 var response = await _mediator.Send(webhookEvent, ct);
//                 
// #pragma warning disable 4014
//                 // Let this fire in the background, after we've confirmed that incoming data
//                 // is valid and dispatched the resulting converted object, it's out of scope 
//                 // for the WebReceiver
//                 _mediator.Publish(response, ct).ConfigureAwait(false);
// #pragma warning restore 4014
//                 return new AcceptedResult();
//             }
//             catch (Exception e)
//             {
//                 _logger.LogError("Error while getting Download job from transformer: {@E}", e);
//                 return new BadRequestResult();
//             }
        }

        private Action<FileInfo> ImplementationOnFinished(object source, FileInfo fileinfo)
        {
            Console.WriteLine(fileinfo);
            throw new NotImplementedException();
        }

        // [HttpGet]
        // public async Task<IActionResult> ReceiveAsync([FromBody] TY webhook, CancellationToken ct = default)
        // {
        //     // IEnumerable<DownloadJob> dlJobs = await _dlService.GenerateDownloadJobsFromWebhookAsync(webhook, ct).ConfigureAwait(false);
        //     foreach (var downloadJob in dlJobs)
        //     {
        //         await _mediator.Publish(downloadJob, ct).ConfigureAwait(false);
        //     }
        //
        //     return Accepted();
        // }
    }
}