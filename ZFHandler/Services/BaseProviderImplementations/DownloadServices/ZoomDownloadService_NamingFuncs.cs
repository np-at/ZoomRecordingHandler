// using System.Globalization;
// using System.Text;
// using System.Text.RegularExpressions;
// using NodaTime;
// using NodaTime.TimeZones;
//
// namespace ZFHandler.Services.BaseProviderImplementations.DownloadServices
// {
//     public  partial class ZoomDownloadService
//     {
//         private  readonly Regex _invalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");
//
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
//             return _invalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
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
//             return _invalidFileNameChars.Replace(sb.ToString(), string.Empty).Replace(" ", "_");
//         }
//         
//     }
// }