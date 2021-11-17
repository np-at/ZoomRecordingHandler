// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using ZFHandler.Models;
// using ZFHandler.Models.ConfigurationSchemas;
// using ZoomFileManager.Models;
// using ZoomFileManager.Services;
// using ZoomFileManager.Services.Providers;
// using ZoomFileManager.Services.Providers.UploadServices;
//
// namespace ZoomFileManager.BackgroundServices
// {
//     public class UploadJobProcessingService : BackgroundService
//     {
//         private readonly ILogger<UploadJobProcessingService> _logger;
//         private readonly ProcessingChannel _processingChannel;
//         private readonly IServiceProvider _serviceProvider;
//         private readonly UploadTarget[] _uploadTargets;
//         public UploadJobProcessingService(ILogger<UploadJobProcessingService> logger, ProcessingChannel processingChannel, IServiceProvider serviceProvider, UploadTarget[] uploadTargets)
//         {
//             _logger = logger;
//             _processingChannel = processingChannel;
//             _serviceProvider = serviceProvider;
//             _uploadTargets = uploadTargets;
//         }
//
//         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//         {
//             try
//             {
//                 while (!stoppingToken.IsCancellationRequested)
//                 {
//                     using var scope = _serviceProvider.CreateScope();
//                     var uploadJob = await _processingChannel.ReadUploadJobAsync(stoppingToken).ConfigureAwait(false);
//                     var reqService = uploadJob.JobType switch
//                     {
//                         JobType.OnedriveUser => typeof(OnedriveUserProvider)
//                     };
//                     object? processor = scope.ServiceProvider.GetRequiredService<IUploadService>();
//                     try
//                     {
//                         
//                     }
//                     catch (Exception e)
//                     {
//                         Console.WriteLine(e);
//                         throw;
//                     }
//                 }
//             }
//             finally
//             {
//                 
//             }
//         }
//     }
// }