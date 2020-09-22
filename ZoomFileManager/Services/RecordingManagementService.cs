using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public class RecordingManagementService : IDisposable
    {
        private readonly PhysicalFileProvider _fileProvider;
        private readonly Odru _odru;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Regex _invalidFileNameChars = new Regex("[\\\\/:\"*?<>|]+");
        private readonly Regex _extensionRegex =  new Regex("\\.[^.]+$");
        private readonly ILogger<RecordingManagementService> _logger;

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

        private IEnumerable<(HttpRequestMessage requestMessage, string fileName, string? folderName)>
            GenerateZoomApiRequestsFromWebhook(ZoomWebhookEvent webhookEvent,
                Func<RecordingFile, string> fileNameTransformationFunc,
                Func<ZoomWebhookEvent, string> folderNameTransformationFunc)
        {
            if (webhookEvent.Payload?.Object?.RecordingFiles == null)
                throw new NullReferenceException();

            var requests = new List<(HttpRequestMessage requestMessage, string name, string? folderName)>();
            foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, item.DownloadUrl);
                if (!string.IsNullOrWhiteSpace(webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken))
                    req.Headers.Authorization =
                        AuthenticationHeaderValue.Parse($"Bearer ${webhookEvent.DownloadToken ?? webhookEvent.Payload.DownloadToken}");
                req.Headers.Add("Accept", "application/json");
                requests.Add((req, fileNameTransformationFunc(item), folderNameTransformationFunc(webhookEvent)));
                req.Dispose();
            }

            return requests.ToArray();
        }

        private string ExampleFolderNameTransformationFunc(ZoomWebhookEvent webhookEvent)
        {
            string st =
                $"{webhookEvent.Payload.Object.StartTime.ToLocalTime().ToString("yy_MM_dd-hhmm-", CultureInfo.InvariantCulture)}{webhookEvent.Payload.Object.Topic}-{webhookEvent.Payload.Object.HostEmail}";
            return _invalidFileNameChars.Replace(st, string.Empty);
        }

        private string ExampleNameTransformationFunc(RecordingFile recordingFile)
        {
            var sb = new StringBuilder();
            sb.Append(recordingFile.Id);
            sb.Append(
                recordingFile.RecordingStart.ToLocalTime().ToString("T", CultureInfo.InvariantCulture));
            sb.Append("." + recordingFile.FileType);
            
            return _invalidFileNameChars.Replace(sb.ToString(), string.Empty);
        }

        internal async Task DownloadFilesFromWebookAsync(ZoomWebhookEvent webhookEvent)
        {
            var requests = GenerateZoomApiRequestsFromWebhook(webhookEvent, ExampleNameTransformationFunc,
                ExampleFolderNameTransformationFunc);
            List<Task<IFileInfo>> tasks = new List<Task<IFileInfo>>();
            
            foreach ((var requestMessage, var fileName, string? folderName) in requests)
            {
                tasks.Add(DownloadFileAsync(requestMessage, fileName, folderName));
                requestMessage.Dispose();
            }

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
                var relPath = Path.GetRelativePath(_fileProvider.Root, file.PhysicalPath).Split(file.Name)[0];
                uploadTasks.Add(_odru.PutFileAsync(file, relPath));
            }

            var c = Task.WhenAll(uploadTasks);
            try
            {
                await c;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                List<Exception> exceptions = new List<Exception>();
                foreach (var file in processedFiles)
                {
                    try
                    {
                        File.Delete(file.PhysicalPath);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        
                    }
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
        private async Task<bool> IsFileLocked(IFileInfo file)
        {
            try
            {
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


            //file is not locked
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="fileName"></param>
        /// <param name="relativePath"></param>
        /// <param name="force"></param>
        /// <param name="failOnExists"></param>
        /// <returns></returns>
        private async Task<IFileInfo> DownloadFileAsync(HttpRequestMessage httpRequest, string fileName, string? relativePath,
            bool force = false, bool failOnExists = false)
        {
            try
            {
                string? fullPath = Path.Join(relativePath, fileName);
                IFileInfo fileInfo;
                try
                {
                    fileInfo = _fileProvider.GetFileInfo(fullPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                try
                {
                    Directory.CreateDirectory(Path.Join(_fileProvider.Root, relativePath));
                }
                catch (Exception e)
                {
                    _logger.LogError("Error creating directory", e);
                    throw;
                }

                if (fileInfo.Exists)
                {
                    if (failOnExists)
                    {
                        _logger.LogError("File already exists.  Set 'failOnExists' = false to avoid this behavior");
                        httpRequest.Dispose();
                        throw new IOException();
                    }

                    int iterations = 0;
                    var fileLocked = !force || await IsFileLocked(fullPath);
                    while (fileLocked && iterations < 100)
                    {
                        
                        iterations++;
                        
                        var testPath = _extensionRegex.Replace(fullPath, match => ($"({iterations.ToString()})" + match.Value));
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

                using var client = _httpClientFactory.CreateClient();

                using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                await using var stream = await response.Content.ReadAsStreamAsync();

                await using var fs = File.Create(fileInfo.PhysicalPath);
                //stream.Seek(0, SeekOrigin.Begin);

                await stream.CopyToAsync(fs);

                _logger.LogInformation($"File saved as [{fileInfo.PhysicalPath}]");
                httpRequest.Dispose();
                return fileInfo;
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

            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug("Sent request", httpRequest);

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync();

            _logger.LogError("Error in download file request", response);
            throw new HttpRequestException(
                $"Error in download file request, received ${response.StatusCode} in response to ${response.RequestMessage}");
        }
    }
}