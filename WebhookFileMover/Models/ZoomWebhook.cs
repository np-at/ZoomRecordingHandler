﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Models
{
    // [GenericTypeControllerFeatureProvider.ApiEntityAttribute]
    public class ZoomWebhookTransformer : IWebhookDownloadJobTransformer<ZoomWebhook>
    {
        public async ValueTask<IEnumerable<DownloadJob>> TransformWebhook(ZoomWebhook webhook,
            CancellationToken cancellationToken = default) => await
            ZoomWebhook.ConvertToDownloadJobAsync(webhook, cancellationToken);

    }
    public class ZoomWebhook
    {
        /// <summary>
        /// Use this token along with the `download_url` to download the  Cloud Recording via an
        /// [OAuth
        /// app](https://marketplace.zoom.us/docs/guides/getting-started/app-types/create-oauth-app).
        /// This token only lasts for 24 hours after generation and thus, you can only download the
        /// file within 24 hours of receiving the "recording.completed" event notification.
        ///
        /// You can either include the `download_token` as a query parameter or pass it as a Bearer
        /// token in the Authorization header of your HTTP request.
        /// #### Using Authorization Header (Recommended)
        ///
        /// ```
        /// curl --request GET \
        /// --url {download_url} \
        /// --header 'authorization: Bearer {download_token} \
        /// --header 'content-type: application/json'
        /// ```
        ///
        /// #### Using Query Parameter
        /// The URL to download this type of recording will follow this structure:
        /// `{download_url}/access_token={download_token}`
        ///
        /// Example: `https://zoom.us/recording/download/bdfdgdgaccess_token=abvdoerbfg`
        /// </summary>
        [JsonPropertyName("download_token")]
        public string? DownloadToken { get; set; }

        /// <summary>
        /// Name of the event.
        /// </summary>
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("payload")]
        public Payload? Payload { get; set; }

        // public static IEnumerable<DownloadJob> ConvertToDownloadJobs(ZoomWebhook input)
        // {
        //     if (input?.Payload?.Object?.RecordingFiles == null)
        //         throw new NullReferenceException("webhook event was null somehow");
        //     var downloadJobs = new List<DownloadJob>();
        //     foreach (var item in input.Payload.Object.RecordingFiles)
        //     {
        //         var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
        //         req.Headers.Add("authorization",
        //             $"Bearer {input.DownloadToken ?? input.Payload.DownloadToken}");
        //         req.Headers.Add("Accept", "*/*");
        //         // req.Headers.Add("content-type", "application/json");
        //         if (item == null)
        //         {
        //             // Log.Error("null Recording file, wtf?");
        //             throw new Exception();
        //         }
        //
        //         var dlJob = new DownloadJob()
        //         {
        //             Message = req,
        //             DestinationFileName = NameTransformationFunc(item),
        //             DestinationFolderPath = FolderNameTransformationFunc(input)
        //         };
        //         downloadJobs.Add(dlJob);
        //     }
        //   
        //     return downloadJobs;
        // }
        public static ValueTask<IEnumerable<DownloadJob>> ConvertToDownloadJobAsync(ZoomWebhook input,
            CancellationToken? ct = default)
        {
            if (input?.Payload?.Object?.RecordingFiles == null)
                throw new NullReferenceException("webhook event was null somehow");
            var downloadJobs = new List<DownloadJob>();
            foreach (var item in input.Payload.Object.RecordingFiles)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
                req.Headers.Add("authorization",
                    $"Bearer {input?.DownloadToken ?? input?.Payload?.DownloadToken ?? throw new NullReferenceException(" Authorization Download Token not found in webhook")}");
                req.Headers.Add("Accept", "*/*");
                // req.Headers.Add("content-type", "application/json");
                if (item == null)
                {
                    // Log.Error("null Recording file, wtf?");
                    throw new Exception();
                }

                var dlJob = new DownloadJob()
                {
                    Message = req,
                    DestinationFileName = NameTransformationFunc(item),
                    DestinationFolderPath = InvalidFileNameChars.Replace($"{input?.Payload?.Object?.Topic ?? "Recording"}-{input?.Payload?.Object?.HostEmail ?? input?.Payload?.AccountId ?? string.Empty}",string.Empty).Replace(' ', '_')
                };
                downloadJobs.Add(dlJob);
            }
          
            return new ValueTask<IEnumerable<DownloadJob>>(downloadJobs);
            
        }

        private static readonly Regex InvalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");

        public static string FolderNameTransformationFunc(ZoomWebhook webhookEvent)
        {
            DateTimeZone usingTimeZone;
            try
            {
                usingTimeZone =
                    DateTimeZoneProviders.Tzdb[webhookEvent.Payload?.Object?.Timezone ?? "America/Los_Angeles"];
            }
            catch (DateTimeZoneNotFoundException)
            {
                usingTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
            }

            var offset =
                usingTimeZone.GetUtcOffset(webhookEvent.Payload?.Object?.StartTime.ToInstant() ?? new Instant());
            var offsetSpan = offset.ToTimeSpan();
            string st =
                $"{webhookEvent?.Payload?.Object?.StartTime.UtcDateTime.Add(offsetSpan).ToString("yy_MM_dd-HHmm-", CultureInfo.InvariantCulture)}{webhookEvent?.Payload?.Object?.Topic ?? "Recording"}-{webhookEvent?.Payload?.Object?.HostEmail ?? webhookEvent?.Payload?.AccountId ?? string.Empty}";
            return InvalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
        }

        public static string NameTransformationFunc(RecordingFile recordingFile)
        {
            var sb = new StringBuilder();
            sb.Append(recordingFile?.Id ?? recordingFile?.FileType ?? string.Empty);
            sb.Append(
                recordingFile?.RecordingStart.ToLocalTime().ToString("T", CultureInfo.InvariantCulture) ?? "_");
            sb.Append($".{recordingFile?.FileType}");

            return InvalidFileNameChars.Replace(sb.ToString(), string.Empty).Replace(" ", "_");
        }
    }

    public partial class Payload
    {
        /// <summary>
        /// Account Id of the user (host / co-host) who ended the meeting and also completed the
        /// recording.
        /// </summary>
        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("object")]
        public Object? Object { get; set; }
        
        [JsonPropertyName("event")]
        public string? Event { get; set; }
        
        [JsonPropertyName("download_token")]
        public string? DownloadToken { get; set; }
    }

    public partial class Object
    {
        /// <summary>
        /// Duration of the recording.
        /// </summary>
        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        /// <summary>
        /// Email address of the host.
        /// </summary>
        [JsonPropertyName("host_email")]
        public string? HostEmail { get; set; }

        /// <summary>
        /// ID of the user who is set as the host of the meeting.
        /// </summary>
        [JsonPropertyName("host_id")]
        public string? HostId { get; set; }

        /// <summary>
        /// Unique Identifier of the Meeting/ Webinar that was being recorded.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// The number of recording files recovered.
        /// </summary>
        [JsonPropertyName("recording_count")]
        public int RecordingCount { get; set; }

        /// <summary>
        /// Array of recording file objects recovered.
        /// </summary>
        [JsonPropertyName("recording_files")]
        public IEnumerable<RecordingFile>? RecordingFiles { get; set; }

        /// <summary>
        /// The URL of the recording using which approved users can view the recording.
        /// </summary>
        [JsonPropertyName("share_url")]
        public string? ShareUrl { get; set; }

        /// <summary>
        /// Meeting start time.
        /// </summary>
        [JsonPropertyName("start_time")]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Timezone to format the meeting start time.
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Meeting topic.
        /// </summary>
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        /// <summary>
        /// The total size of the recording in bytes.
        /// </summary>
        [JsonPropertyName("total_size")]
        public long TotalSize { get; set; }

        /// <summary>
        /// Meeting or Webinar Types.
        ///
        /// If the recording is of a meeting, the type can be one of the following Meeting
        /// types:<br>`1` - Instant meeting.<br>`2` - Scheduled meeting.<br>`3` - Recurring meeting
        /// with no fixed time.<br>
        /// `4` - Meeting created using Personal Meeting ID.<br>
        /// `7` - Personal Audio Conference
        /// ([PAC](https://support.zoom.us/hc/en-us/articles/204517069-Getting-Started-with-Personal-Audio-Conference)).<br>
        /// `8` - Recurring meeting with fixed time.
        ///
        /// If the recording is of a Webinar, the type can be one of the following Webinar Types:<br>
        /// `5` - Webinar<br> `6` - Recurring Webinar without a fixed time<br> `9` - Recurring
        /// Webinar with a fixed time.
        /// </summary>
        [JsonPropertyName("type")]
        public int Type { get; set; }

        /// <summary>
        /// Universally unique identifier of the Meeting/Webinar instance that was recorded.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }
    }

    public partial class RecordingFile
    {
        /// <summary>
        /// The URL using which the file can be downloaded.
        /// </summary>
        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// The size of the recording file in bytes.
        /// </summary>
        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        /// <summary>
        /// The type of file.
        /// </summary>
        [JsonPropertyName("file_type")]
        public string? FileType { get; set; }

        /// <summary>
        /// Unique Identifier for Recording File. Recording File ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Unique Identifier of the meeting.
        /// </summary>
        [JsonPropertyName("meeting_id")]
        public string? MeetingId { get; set; }

        /// <summary>
        /// The URL of the file using which it can be opened and played.
        /// </summary>
        [JsonPropertyName("play_url")]
        public string? PlayUrl { get; set; }

        /// <summary>
        /// The date and time at which recording ended.
        /// </summary>
        [JsonPropertyName("recording_end")]
        public string? RecordingEnd { get; set; }

        /// <summary>
        /// The date and time at which recording started.
        /// </summary>
        [JsonPropertyName("recording_start")]
        public DateTimeOffset RecordingStart { get; set; }

        /// <summary>
        /// The type of the recording.
        /// </summary>
        [JsonPropertyName("recording_type")]
        public string? RecordingType { get; set; }

        /// <summary>
        /// Status of the recording. <br />`completed`: Recording has been completed.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}