﻿// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using ZoomFileManager.Models;
//
//    var coordinate = Coordinate.FromJson(jsonString);

namespace ZoomFileManager.Models
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class ZoomWebhookEvent
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
        /// `{download_url}/?access_token={download_token}`
        ///
        /// Example: `https://zoom.us/recording/download/bdfdgdg?access_token=abvdoerbfg`
        /// </summary>
        [JsonProperty("download_token", NullValueHandling = NullValueHandling.Ignore)]
        public string DownloadToken { get; set; }

        /// <summary>
        /// Name of the event.
        /// </summary>
        [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
        public string Event { get; set; }

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public Payload Payload { get; set; }
    }

    public partial class Payload
    {
        /// <summary>
        /// Account Id of the user (host / co-host) who ended the meeting and also completed the
        /// recording.
        /// </summary>
        [JsonProperty("account_id", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountId { get; set; }

        [JsonProperty("object", NullValueHandling = NullValueHandling.Ignore)]
        public Object Object { get; set; }
    }

    public partial class Object
    {
        /// <summary>
        /// Duration of the recording.
        /// </summary>
        [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
        public long? Duration { get; set; }

        /// <summary>
        /// Email address of the host.
        /// </summary>
        [JsonProperty("host_email", NullValueHandling = NullValueHandling.Ignore)]
        public string HostEmail { get; set; }

        /// <summary>
        /// ID of the user who is set as the host of the meeting.
        /// </summary>
        [JsonProperty("host_id", NullValueHandling = NullValueHandling.Ignore)]
        public string HostId { get; set; }

        /// <summary>
        /// Unique Identifier of the Meeting/ Webinar that was being recorded.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        /// <summary>
        /// The number of recording files recovered.
        /// </summary>
        [JsonProperty("recording_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? RecordingCount { get; set; }

        /// <summary>
        /// Array of recording file objects recovered.
        /// </summary>
        [JsonProperty("recording_files", NullValueHandling = NullValueHandling.Ignore)]
        public RecordingFile[] RecordingFiles { get; set; }

        /// <summary>
        /// The URL of the recording using which approved users can view the recording.
        /// </summary>
        [JsonProperty("share_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ShareUrl { get; set; }

        /// <summary>
        /// Meeting start time.
        /// </summary>
        [JsonProperty("start_time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Timezone to format the meeting start time.
        /// </summary>
        [JsonProperty("timezone", NullValueHandling = NullValueHandling.Ignore)]
        public string Timezone { get; set; }

        /// <summary>
        /// Meeting topic.
        /// </summary>
        [JsonProperty("topic", NullValueHandling = NullValueHandling.Ignore)]
        public string Topic { get; set; }

        /// <summary>
        /// The total size of the recording in bytes.
        /// </summary>
        [JsonProperty("total_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? TotalSize { get; set; }

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
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public long? Type { get; set; }

        /// <summary>
        /// Universally unique identifier of the Meeting/Webinar instance that was recorded.
        /// </summary>
        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string Uuid { get; set; }
    }

    public partial class RecordingFile
    {
        /// <summary>
        /// The URL using which the file can be downloaded.
        /// </summary>
        [JsonProperty("download_url", NullValueHandling = NullValueHandling.Ignore)]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// The size of the recording file in bytes.
        /// </summary>
        [JsonProperty("file_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? FileSize { get; set; }

        /// <summary>
        /// The type of file.
        /// </summary>
        [JsonProperty("file_type", NullValueHandling = NullValueHandling.Ignore)]
        public string FileType { get; set; }

        /// <summary>
        /// Unique Identifier for Recording File. Recording File ID.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Unique Identifier of the meeting.
        /// </summary>
        [JsonProperty("meeting_id", NullValueHandling = NullValueHandling.Ignore)]
        public string MeetingId { get; set; }

        /// <summary>
        /// The URL of the file using which it can be opened and played.
        /// </summary>
        [JsonProperty("play_url", NullValueHandling = NullValueHandling.Ignore)]
        public string PlayUrl { get; set; }

        /// <summary>
        /// The date and time at which recording ended.
        /// </summary>
        [JsonProperty("recording_end", NullValueHandling = NullValueHandling.Ignore)]
        public string RecordingEnd { get; set; }

        /// <summary>
        /// The date and time at which recording started.
        /// </summary>
        [JsonProperty("recording_start", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? RecordingStart { get; set; }

        /// <summary>
        /// The type of the recording.
        /// </summary>
        [JsonProperty("recording_type", NullValueHandling = NullValueHandling.Ignore)]
        public string RecordingType { get; set; }

        /// <summary>
        /// Status of the recording. <br>`completed`: Recording has been completed.
        /// </summary>
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
    }

    public partial class ZoomWebhookEvent
    {
        public static ZoomWebhookEvent FromJson(string json) => JsonConvert.DeserializeObject<ZoomWebhookEvent>(json, ZoomFileManager.Models.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ZoomWebhookEvent self) => JsonConvert.SerializeObject(self, ZoomFileManager.Models.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
