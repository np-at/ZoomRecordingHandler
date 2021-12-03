// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Linq;
// using System.Net.Http;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading;
// using System.Threading.Tasks;
// using MediatR;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using NodaTime.TimeZones;
// using ZFHandler.Mdtr.Commands;
//
// namespace ZFHandler.Services.BaseProviderImplementations.DownloadServices
// {
//     public abstract class DlService<T> : IDownloadService<T>
//     {
//         // if ValidationFunction isn't provided, assume that no validation is required?
//         public virtual ValueTask<bool> ValidateWebhookParametersAsync(T webhookEvent)
//         {
//             return new ValueTask<bool>(true);
//         }
//
//         public abstract Task<IEnumerable<DownloadJob>> GenerateDownloadJobsFromWebhookAsync(T webhookEvent,
//             CancellationToken ct = default);
//     }
//
//     public class ZoomWebhookHandler : IRequestHandler<Zoominput, DownloadJobBatch>
//     {
//         private static readonly Regex InvalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");
//
//         private readonly IMediator _mediator;
//         private readonly ILogger<ZoomWebhookHandler> _logger;
//         public ZoomWebhookHandler(IMediator mediator, ILogger<ZoomWebhookHandler> logger)
//         {
//             _mediator = mediator;
//             _logger = logger;
//         }
//         public async Task<DownloadJobBatch> Handle(Zoominput request, CancellationToken cancellationToken)
//         {
//             try
//             {
//                 var dlJobs = await GenerateDownloadJobsFromWebhookAsync(request, cancellationToken);
//                 var downloadJobs = dlJobs as DownloadJob[] ?? dlJobs.ToArray();
//                 foreach (var downloadJob in downloadJobs)
//                 {
//                     await _mediator.Publish(downloadJob, cancellationToken).ConfigureAwait(false);
//                 }
//
//                 return new DownloadJobBatch()
//                 {
//                     Jobs = downloadJobs
//                 };
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError("error generating dljob for webhook: {@Request} \n Inner Error: {@Error}", request, ex);
//                 throw;
//             }
//         }
//         private  async Task<IEnumerable<DownloadJob>> GenerateDownloadJobsFromWebhookAsync(
//             Zoominput webhookEvent, CancellationToken ct = default)
//         {
//             if (webhookEvent?.Payload?.Object?.RecordingFiles == null)
//                 throw new NullReferenceException("webhook event was null somehow");
//             var downloadJobs = new List<DownloadJob>();
//             foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
//             {
//                 var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
//                 req.Headers.Add("authorization",
//                     $"Bearer {webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
//                 req.Headers.Add("Accept", "*/*");
//                 // req.Headers.Add("content-type", "application/json");
//                 if (item == null)
//                 {
//                     Log.Error("null Recording file, wtf?");
//                     continue;
//                 }
//
//                 var dlJob = new DownloadJob()
//                 {
//                     message = req,
//                     DestinationFileName = NameTransformationFunc(item),
//                     DestinationFolderPath = FolderNameTransformationFunc(webhookEvent)
//                 };
//                 downloadJobs.Add(dlJob);
//             }
//
//             return downloadJobs;
//         }
//         private  string FolderNameTransformationFunc(Zoominput webhookEvent)
//         {
//             DateTimeZone usingTimeZone;
//             try
//             {
//                 usingTimeZone =
//                     DateTimeZoneProviders.Tzdb[webhookEvent.Payload?.Object?.Timezone ?? "America/Los_Angeles"];
//             }
//             catch (DateTimeZoneNotFoundException)
//             {
//                 usingTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
//             }
//
//             var offset =
//                 usingTimeZone.GetUtcOffset(webhookEvent.Payload?.Object?.StartTime.ToInstant() ?? new Instant());
//             var offsetSpan = offset.ToTimeSpan();
//             string st =
//                 $"{webhookEvent?.Payload?.Object?.StartTime.UtcDateTime.Add(offsetSpan).ToString("yy_MM_dd-HHmm-", CultureInfo.InvariantCulture)}{webhookEvent?.Payload?.Object?.Topic ?? "Recording"}-{webhookEvent?.Payload?.Object?.HostEmail ?? webhookEvent?.Payload?.AccountId ?? string.Empty}";
//             return InvalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
//         }
//
//         private string NameTransformationFunc(RecordingFile recordingFile)
//         {
//             var sb = new StringBuilder();
//             sb.Append(recordingFile?.Id ?? recordingFile?.FileType ?? string.Empty);
//             sb.Append(
//                 recordingFile?.RecordingStart.ToLocalTime().ToString("T", CultureInfo.InvariantCulture) ?? "_");
//             sb.Append($".{recordingFile?.FileType}");
//
//             return InvalidFileNameChars.Replace(sb.ToString(), string.Empty).Replace(" ", "_");
//         }
//     }
//
//     public partial class ZoomDownloadService : DlService<Zoominput>
//     {
//         // private readonly Regex _extensionRegex = new("\\.[^.]+$");
//
//         // private readonly PhysicalFileProvider _fileProvider;
//         // private readonly ILogger<ZoomDownloadService> _logger;
//         // private readonly HttpClient _httpClient;
//         // private readonly IHttpClientFactory _httpClientFactory;
//
//         // public ZoomDownloadService(PhysicalFileProvider fileProvider, ILogger<ZoomDownloadService> logger, IHttpClientFactory httpClientFactory)
//         // {
//         //     _fileProvider = fileProvider;
//         //     _logger = logger;
//         //     _httpClientFactory = httpClientFactory;
//         //     _httpClient = httpClientFactory.CreateClient();
//         // }
//         
//         
//         /// <summary>
//         /// Explicitly overriding Validation function for clarity 
//         /// </summary>
//         /// <param name="webhookEvent"></param>
//         /// <returns></returns>
//         public override ValueTask<bool> ValidateWebhookParametersAsync(Zoominput webhookEvent)
//         {
//             return ValueTask.FromResult<bool>(true);
//         }
//
//
//         public override async Task<IEnumerable<DownloadJob>> GenerateDownloadJobsFromWebhookAsync(
//             Zoominput webhookEvent, CancellationToken ct = default)
//         {
//             if (webhookEvent?.Payload?.Object?.RecordingFiles == null)
//                 throw new NullReferenceException("webhook event was null somehow");
//             var downloadJobs = new List<DownloadJob>();
//             foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
//             {
//                 var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
//                 req.Headers.Add("authorization",
//                     $"Bearer {webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
//                 req.Headers.Add("Accept", "*/*");
//                 // req.Headers.Add("content-type", "application/json");
//                 if (item == null)
//                 {
//                     Log.Error("null Recording file, wtf?");
//                     continue;
//                 }
//
//                 var dlJob = new DownloadJob()
//                 {
//                     message = req,
//                     DestinationFileName = NameTransformationFunc(item),
//                     DestinationFolderPath = FolderNameTransformationFunc(webhookEvent)
//                 };
//                 downloadJobs.Add(dlJob);
//             }
//           
//             return downloadJobs;
//         }
//
//
//         private static IEnumerable<DownloadJob> GenerateZoomApiRequestsFromWebhook(Zoominput webhookEvent,
//                 Func<RecordingFile, string> fileNameTransformationFunc,
//                 Func<Zoominput, string> folderNameTransformationFunc)
//         {
//             if (webhookEvent?.Payload?.Object?.RecordingFiles == null)
//                 throw new NullReferenceException("webhook event was null somehow");
//
//             
//             var requests2 = new List<DownloadJob>();
//             foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
//             {
//                 var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
//                 req.Headers.Add("authorization",
//                     $"Bearer {webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
//                 req.Headers.Add("Accept", "*/*");
//                 // req.Headers.Add("content-type", "application/json");
//                 if (item == null)
//                 {
//                     Log.Error("null Recording file, wtf?");
//                     continue;
//                 }
//
//                 var dlJob = new DownloadJob()
//                 {
//                     message = req,
//                     DestinationFileName = fileNameTransformationFunc(item),
//                     DestinationFolderPath = folderNameTransformationFunc(webhookEvent)
//                 };
//                 requests2.Add(dlJob);
//             }
//
//             return requests2;
//         }
//     }
// }