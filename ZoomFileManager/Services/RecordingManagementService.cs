using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using Serilog;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public class RecordingManagementService : IDisposable
    {
        private readonly Regex _extensionRegex = new Regex("\\.[^.]+$");
        private readonly PhysicalFileProvider _fileProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Regex _invalidFileNameChars = new Regex("[\\\\/:\"*?<>|'`]+");
        private readonly ILogger<RecordingManagementService> _logger;
        private readonly Odru _odru;

        public RecordingManagementService(ILogger<RecordingManagementService> logger,
            IHttpClientFactory httpClientFactory, PhysicalFileProvider fileProvider, Odru odru)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _fileProvider = fileProvider;
            _odru = odru;
        }


        public void Dispose()
        {
            _fileProvider.Dispose();
        }

        private static IEnumerable<(HttpRequestMessage requestMessage, string fileName, string? folderName)>
            GenerateZoomApiRequestsFromWebhook(ZoomWebhookEvent webhookEvent,
                Func<RecordingFile, string> fileNameTransformationFunc,
                Func<ZoomWebhookEvent, string> folderNameTransformationFunc)
        {
            if (webhookEvent?.Payload?.Object?.RecordingFiles == null)
                throw new NullReferenceException("webhook event was null somehow");

            var requests = new List<(HttpRequestMessage requestMessage, string name, string? folderName)>();
            foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, item?.DownloadUrl ?? string.Empty);
                // if (!string.IsNullOrWhiteSpace(webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken))
                //     req.Headers.Authorization =
                //         AuthenticationHeaderValue.Parse($"Bearer ${webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
                req.Headers.Add("authorization",
                    $"Bearer {webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
                req.Headers.Add("Accept", "*/*");
                // req.Headers.Add("content-type", "application/json");
                if (item == null)
                {
                    Log.Error("null Recording file, wtf?");
                    continue;
                }

                requests.Add((req, fileNameTransformationFunc(item), folderNameTransformationFunc(webhookEvent)));
            }

            return requests.ToArray();
        }

        private string ExampleFolderNameTransformationFunc(ZoomWebhookEvent webhookEvent)
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

            var offset = usingTimeZone.GetUtcOffset(webhookEvent.Payload?.Object?.StartTime.ToInstant() ?? new Instant());
            var offsetSpan = offset.ToTimeSpan();
            string st =
                $"{webhookEvent?.Payload?.Object?.StartTime.UtcDateTime.Add(offsetSpan).ToString("yy_MM_dd-HHmm-", CultureInfo.InvariantCulture)}{webhookEvent?.Payload?.Object?.Topic ?? "Recording"}-{webhookEvent?.Payload?.Object?.HostEmail ?? webhookEvent?.Payload?.AccountId ?? string.Empty}";
            return _invalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
        }

        private string ExampleNameTransformationFunc(RecordingFile recordingFile)
        {
            var sb = new StringBuilder();
            sb.Append(recordingFile?.Id ?? recordingFile?.FileType ?? string.Empty);
            sb.Append(
                recordingFile?.RecordingStart.ToLocalTime().ToString("T", CultureInfo.InvariantCulture) ?? "_");
            sb.Append("." + recordingFile?.FileType);

            return _invalidFileNameChars.Replace(sb.ToString(), string.Empty).Replace(" ", "_");
        }

        internal async Task DownloadFilesFromWebookAsync(ZoomWebhookEvent webhookEvent)
        {
            var requests = GenerateZoomApiRequestsFromWebhook(webhookEvent, ExampleNameTransformationFunc,
                ExampleFolderNameTransformationFunc);
            List<Task<IFileInfo>> tasks = new List<Task<IFileInfo>>();

            foreach ((var requestMessage, var fileName, string? folderName) in requests)
                tasks.Add(DownloadFileAsync(requestMessage, fileName, folderName));
            // requestMessage.Dispose();

            var t = Task.WhenAll(tasks);
            IFileInfo[] processedFiles;
            try
            {
                processedFiles = await t;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            List<Task> uploadTasks = new List<Task>();
            foreach (var file in processedFiles)
            {
                string? relPath = Path.GetRelativePath(_fileProvider.Root, file.PhysicalPath).Split(file.Name)[0];
                uploadTasks.Add(_odru.PutFileAsync(file, relPath));
            }

            var c = Task.WhenAll(uploadTasks);
            try
            {
                await c;
            }
            catch (Exception e)
            {
                _logger.LogError("Error processing uploads", e);
                throw;
            }
            finally
            {
                List<Exception> exceptions = new List<Exception>();
                foreach (var file in processedFiles)
                    try
                    {
                        File.Delete(file.PhysicalPath);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }

                if (exceptions.Any())
                    _logger.LogError("error deleting files", exceptions);
            }
        }

        private async Task<bool> IsFileLocked(string filePath)
        {
            try
            {
                var file = _fileProvider.GetFileInfo(filePath);
                await using FileStream stream = File.Open(file.PhysicalPath, FileMode.Open, FileAccess.Read,
                    FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("failure", ex);
                throw;
            }


            //file is not locked
            return false;
        }


        /// <summary>
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="fileName"></param>
        /// <param name="relativePath"></param>
        /// <param name="force"></param>
        /// <param name="failOnExists"></param>
        /// <returns></returns>
        private async Task<IFileInfo> DownloadFileAsync(HttpRequestMessage httpRequest, string fileName,
            string? relativePath,
            bool force = false, bool failOnExists = false)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    string? fullPath = Path.Join(relativePath, fileName);
                    IFileInfo fileInfo;
                  

                    try
                    {
                        Directory.CreateDirectory(Path.Join(_fileProvider.Root, relativePath));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Error creating directory", e);
                        throw;
                    }
                    try
                    {
                        fileInfo = _fileProvider.GetFileInfo(fullPath);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("error while getting file info", e);
                        throw;
                    }
                    if (fileInfo.Exists)
                    {
                        if (failOnExists)
                        {
                            _logger.LogError("File already exists.  Set 'failOnExists' = false to avoid this behavior");
                            // httpRequest.Dispose();
                            throw new IOException();
                        }

                        int iterations = 0;
                        bool fileLocked = !force || await IsFileLocked(fullPath);
                        while (fileLocked && iterations < 100)
                        {
                            iterations++;

                            string? testPath = _extensionRegex.Replace(fullPath,
                                match => $"({iterations.ToString()})" + match.Value);
                            try
                            {
                                if (!_fileProvider.GetFileInfo(testPath).Exists)
                                {
                                    fullPath = testPath;
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }

                            if (!force) continue;
                            fileLocked = await IsFileLocked(testPath);
                            if (fileLocked) continue;
                            fullPath = testPath;
                            break;
                        }

                        try
                        {
                            fileInfo = _fileProvider.GetFileInfo(fullPath);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("error checking file?", e);
                            throw;
                        }
                    }

                    try
                    {
                        await using var fs = File.Create(fileInfo.PhysicalPath);
                        using var client = _httpClientFactory.CreateClient();

                        using var response =
                            await client?.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                        await using var stream = await response.Content.ReadAsStreamAsync();
                        //stream.Seek(0, SeekOrigin.Begin);

                        await stream.CopyToAsync(fs);

                        _logger.LogInformation($"File saved as [{fileInfo.PhysicalPath}]");
                        return fileInfo;
                    }
                    catch (IOException e)
                    {
                        _logger.LogError("IO ERROR", e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"misc error encountered while creating file",ex);
                        throw;
                    }

                    await Task.Delay(1000);
                }

                throw new IOException("Failed ot create file after 5 attempts");
            }
            catch (Exception e)
            {
                _logger.LogError("error during file download", e);
                throw;
            }
        }

        internal async Task<Stream> GetDownloadAsStreamAsync(HttpRequestMessage httpRequest)
        {
            using var client = _httpClientFactory.CreateClient();

            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug("Sent request", httpRequest);

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync();

            _logger.LogError("Error in download file request", response);
            throw new HttpRequestException(
                $"Error in download file request, received ${response.StatusCode} in response to ${response.RequestMessage}");
        }
    }
}