// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using ZoomFileManager.BackgroundServices;
// using ZoomFileManager.Extensions.CustomBuilders;
// using ZoomFileManager.Services;
//
// namespace ZoomFileManager.Controllers
// {
//     [ApiController]
//     public class WebhookReceiver<T> : ControllerBase where T : IValidator<T>
//     {
//         private readonly ILogger<WebhookReceiver<T>> Logger;
//         private readonly IOptions<WebhookReceiversOptions> Options;
//         private readonly ProcessingChannel ProcessingChannel;
//         private readonly WebhookConfig<T> _config;
//
//         protected WebhookReceiver(ILogger<WebhookReceiver<T>> logger, IOptions<WebhookReceiversOptions> options, WebhookConfig<T> config,
//             ProcessingChannel processingChannel)
//         {
//             Logger = logger;
//             Options = options;
//             _config = config;
//             ProcessingChannel = processingChannel;
//         }
//
//         // public async Task<IActionResult> ReceiveNotificationAsync([FromBody] T webhookEvent,
//         //     CancellationToken cancellationToken = default)
//         // {
//         //     
//         // }
//
//
// }
// }