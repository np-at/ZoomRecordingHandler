﻿// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Linq;
// using System.Net.Http;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.FileProviders;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Microsoft.Graph;
// using NodaTime;
// using NodaTime.Extensions;
// using NodaTime.TimeZones;
// using Serilog;
// using ZoomFileManager.Helpers;
// using ZoomFileManager.Models;
// using Directory = System.IO.Directory;
// using File = System.IO.File;
//
// namespace ZoomFileManager.Services
// {
//     public class RecordingManagementServiceOptions
//     {
//         public string[]? Endpoints { get; set; } = Array.Empty<string>();
//         public string? ReferralUrlBase { get; set; }
//
//         public string[]? AllowedHostEmails { get; set; }
//     }
//
//     public class RecordingManagementService : IDisposable
//     {
//         private readonly Regex _extensionRegex = new("\\.[^.]+$");
//         private readonly PhysicalFileProvider _fileProvider;
//         private readonly IHttpClientFactory _httpClientFactory;
//         private readonly Regex _invalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");
//         private readonly ILogger<RecordingManagementService> _logger;
//         private readonly OneDriveOperationsService _oneDriveOperationsService;
//         private readonly RecordingManagementServiceOptions _serviceOptions;
//         private readonly SlackApiHelpers _slackApiHelpers;
//
//         public RecordingManagementService(ILogger<RecordingManagementService> logger,
//             IHttpClientFactory httpClientFactory, PhysicalFileProvider fileProvider,
//             OneDriveOperationsService oneDriveOperationsService, SlackApiHelpers slackApiHelpers,
//             IOptions<RecordingManagementServiceOptions>? serviceOptions)
//         {
//             _logger = logger;
//             _httpClientFactory = httpClientFactory;
//             _fileProvider = fileProvider;
//             _oneDriveOperationsService = oneDriveOperationsService;
//             _slackApiHelpers = slackApiHelpers;
//             _serviceOptions = serviceOptions?.Value ?? new RecordingManagementServiceOptions();
//         }
//
//
//         public void Dispose()
//         {
//             GC.SuppressFinalize(this);
//             _fileProvider.Dispose();
//         }
//
//         private static IEnumerable<(HttpRequestMessage requestMessage, string fileName, string? folderName)>
//             GenerateZoomApiRequestsFromWebhook(Zoominput webhookEvent,
//                 Func<RecordingFile, string> fileNameTransformationFunc,
//                 Func<Zoominput, string> folderNameTransformationFunc)
//         {
//             if (webhookEvent?.Payload?.Object?.RecordingFiles == null)
//                 throw new NullReferenceException("webhook event was null somehow");
//
//             var requests = new List<(HttpRequestMessage requestMessage, string name, string? folderName)>();
//             foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
//             {
//                 var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
//                 // if (!string.IsNullOrWhiteSpace(webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken))
//                 //     req.Headers.Authorization =
//                 //         AuthenticationHeaderValue.Parse($"Bearer ${webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
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
//                 requests.Add((req, fileNameTransformationFunc(item), folderNameTransformationFunc(webhookEvent)));
//             }
//
//             return requests;
//         }
//
//         private string ExampleFolderNameTransformationFunc(Zoominput webhookEvent)
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
//         private string ExampleNameTransformationFunc(RecordingFile recordingFile)
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
//         private bool IsHostEmailAllowed(string hostEmail)
//         {
//             return _serviceOptions.AllowedHostEmails?.Contains(hostEmail, StringComparer.InvariantCultureIgnoreCase) ??
//                    false;
//         }
//
//         internal async Task DownloadFilesFromWebhookAsync(Zoominput webhookEvent, CancellationToken ct = default)
//         {
//             // if AllowedHostEmails is defined and the current zoom event doesn't have its hostEmail in that list, abort
//             if (_serviceOptions.AllowedHostEmails != null && _serviceOptions.AllowedHostEmails.Any() &&
//                 !IsHostEmailAllowed(webhookEvent.Payload.Object.HostEmail))
//             {
//                 _logger.LogDebug("Received Zoominput with invalid hostEmail address of {EmailAddress}, aborting", webhookEvent.Payload.Object.HostEmail);
//                 return;
//             }
//                 
//
//             var requests = GenerateZoomApiRequestsFromWebhook(webhookEvent, ExampleNameTransformationFunc,
//                 ExampleFolderNameTransformationFunc);
//             List<Task<IFileInfo>> tasks = new();
//
//             foreach ((var requestMessage, var fileName, string? folderName) in requests)
//                 tasks.Add(DownloadFileAsync(requestMessage, fileName, folderName));
//             // requestMessage.Dispose();
//
//             var t = Task.WhenAll(tasks);
//             IFileInfo[] processedFiles;
//             try
//             {
//                 processedFiles = await t.ConfigureAwait(false);
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 throw;
//             }
//
//             List<Task<UploadResult<DriveItem>>> uploadTasks = (from file in processedFiles
//                 let relPath = Path.GetRelativePath(_fileProvider.Root, file.PhysicalPath).Split(file.Name)[0]
//                 select _oneDriveOperationsService.PutFileAsync(file, relPath, ct)).ToList();
//
//             var c = Task.WhenAll(uploadTasks);
//             try
//             {
//                 var items = await c.ConfigureAwait(false);
//                 if (_serviceOptions.Endpoints?.Any() ?? false)
//                     if (items.All(x => x.UploadSucceeded))
//                     {
//                         string itemResponseWebUrl = items.Last().ItemResponse.WebUrl;
//                         string? userId = string.IsNullOrWhiteSpace(webhookEvent.Payload.Object.HostEmail)
//                             ? string.Empty
//                             : await _slackApiHelpers.GetUserIdAsync(webhookEvent.Payload.Object.HostEmail).ConfigureAwait(false);
//                         string? message =
//                             $"{(string.IsNullOrWhiteSpace(webhookEvent.Payload.Object.HostEmail) ? string.Empty : "<@" + userId + '>')}Successfully uploaded recording: {webhookEvent.Payload.Object.Topic}. You can view them using this url: <{_serviceOptions.ReferralUrlBase + itemResponseWebUrl.Remove(itemResponseWebUrl.LastIndexOf('/'))}| onedrive folder link>";
//                         foreach (string notificationEndpoint in _serviceOptions.Endpoints)
//                             await SendWebhookNotification(notificationEndpoint, message).ConfigureAwait(false);
//                     }
//             }
//             catch (Exception e)
//             {
//                 _logger.LogError("Error processing uploads: {Error}", e);
//                 throw;
//             }
//             finally
//             {
//                 List<Exception> exceptions = new();
//                 foreach (var file in processedFiles)
//                     try
//                     {
//                         File.Delete(file.PhysicalPath);
//                     }
//                     catch (Exception e)
//                     {
//                         exceptions.Add(e);
//                     }
//
//                 if (exceptions.Any())
//                     _logger.LogError("error deleting files: {Error}", exceptions);
//             }
//         }
//
//         private async Task<bool> IsFileLocked(string filePath)
//         {
//             try
//             {
//                 var file = _fileProvider.GetFileInfo(filePath);
//                 await using FileStream stream = File.Open(file.PhysicalPath, FileMode.Open, FileAccess.Read,
//                     FileShare.None);
//             }
//             catch (IOException)
//             {
//                 //the file is unavailable because it is:
//                 //still being written to
//                 //or being processed by another thread
//                 //or does not exist (has already been processed)
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError("failure: {Error}", ex);
//                 throw;
//             }
//
//
//             //file is not locked
//             return false;
//         }
//
//
//         /// <summary>
//         /// </summary>
//         /// <param name="httpRequest"></param>
//         /// <param name="fileName"></param>
//         /// <param name="relativePath"></param>
//         /// <param name="force"></param>
//         /// <param name="failOnExists"></param>
//         /// <returns></returns>
//         private async Task<IFileInfo> DownloadFileAsync(HttpRequestMessage httpRequest, string fileName,
//             string? relativePath,
//             bool force = false, bool failOnExists = false)
//         {
//             if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
//             if (fileName == null)
//                 throw new ArgumentNullException(nameof(fileName));
//             try
//             {
//                 string? fullPath = Path.Join(relativePath, fileName);
//                 IFileInfo fileInfo;
//
//
//                 try
//                 {
//                     Directory.CreateDirectory(Path.Join(_fileProvider.Root, relativePath));
//                 }
//                 catch (Exception e)
//                 {
//                     _logger.LogError("Error creating directory: {Error}", e);
//                     throw;
//                 }
//
//                 try
//                 {
//                     fileInfo = _fileProvider.GetFileInfo(fullPath);
//                 }
//                 catch (Exception e)
//                 {
//                     _logger.LogError("error while getting file info: {Error}", e);
//                     throw;
//                 }
//
//                 if (fileInfo.Exists)
//                 {
//                     if (failOnExists)
//                     {
//                         _logger.LogError("File already exists.  Set 'failOnExists' = false to avoid this behavior");
//                         // httpRequest.Dispose();
//                         throw new IOException();
//                     }
//
//                     int iterations = 0;
//                     bool fileLocked = !force || await IsFileLocked(fullPath).ConfigureAwait(false);
//                     while (fileLocked && iterations < 100)
//                     {
//                         iterations++;
//
//                         int iterations1 = iterations;
//                         string? testPath = _extensionRegex.Replace(fullPath,
//                             match => $"({iterations1.ToString()})" + match.Value);
//                         try
//                         {
//                             if (!_fileProvider.GetFileInfo(testPath).Exists)
//                             {
//                                 fullPath = testPath;
//                                 break;
//                             }
//                         }
//                         catch (Exception e)
//                         {
//                             Console.WriteLine(e);
//                             throw;
//                         }
//
//                         if (!force) continue;
//                         fileLocked = await IsFileLocked(testPath).ConfigureAwait(false);
//                         if (fileLocked) continue;
//                         fullPath = testPath;
//                         break;
//                     }
//
//                     try
//                     {
//                         fileInfo = _fileProvider.GetFileInfo(fullPath);
//                     }
//                     catch (Exception e)
//                     {
//                         _logger.LogError("error checking file?: {Error}", e);
//                         throw;
//                     }
//                 }
//
//                 try
//                 {
//                     await using var fs = File.Create(fileInfo.PhysicalPath);
//                     using var client = _httpClientFactory.CreateClient();
//                     using var s = _httpClientFactory.CreateClient("test");
//
//                     using var response =
//                         await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
//                     if (!response.IsSuccessStatusCode)
//                         response.EnsureSuccessStatusCode();
//                     await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
//                     //stream.Seek(0, SeekOrigin.Begin);
//
//                     await stream.CopyToAsync(fs).ConfigureAwait(false);
//
//                     _logger.LogInformation("File saved as [{PhysicalPath}]", fileInfo.PhysicalPath);
//                     return fileInfo;
//                 }
//                 catch (IOException e)
//                 {
//                     _logger.LogError(e,"IO ERROR: {E}", e.Message);
//                     throw;
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError("misc error encountered while creating file: {Exception}", ex);
//                     throw;
//                 }
//
//                 // await Task.Delay(1000);
//
//
//                 throw new IOException("Failed to create file after 5 attempts");
//             }
//             catch (Exception e)
//             {
//                 _logger.LogError("error during file download", e);
//                 throw;
//             }
//         }
//
//         private async Task SendWebhookNotification(string endpoint, string message)
//         {
//             string? jsonMessage = $"{{\"text\": \"{message}\"}}";
//             using var client = _httpClientFactory.CreateClient();
//             var responseMessage = await client.PostAsync(endpoint,
//                 new StringContent(jsonMessage, Encoding.UTF8, "application/json")).ConfigureAwait(false);
//             if (!responseMessage.IsSuccessStatusCode)
//                 _logger.LogError(
//                     "Unsuccessful in activating notification provider at endpoint: {Endpoint} \n for message: \n {Message}", endpoint, message);
//         }
//
//         internal async Task<Stream> GetDownloadAsStreamAsync(HttpRequestMessage httpRequest)
//         {
//             using var client = _httpClientFactory.CreateClient();
//
//             using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
//             _logger.LogDebug("Sent request", httpRequest);
//
//             if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
//
//             _logger.LogError("Error in download file request: {Response}", response);
//             throw new HttpRequestException(
//                 $"Error in download file request, received ${response.StatusCode} in response to ${response.RequestMessage}");
//         }
//     }
// }