using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public class RecordingManagementService : IDisposable
    {
        private readonly ILogger<RecordingManagementService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PhysicalFileProvider _fileProvider;
        public RecordingManagementService(ILogger<RecordingManagementService> logger, IHttpClientFactory httpClientFactory, PhysicalFileProvider fileProvider)
        {
            this._logger = logger;
            _httpClientFactory = httpClientFactory;
            _fileProvider = fileProvider;
        }
        internal IEnumerable<(HttpRequestMessage requestMessage, string name)> GenerateZoomApiRequestsFromWebhook(ZoomWebhookEvent webhookEvent, Func<RecordingFile, string> nameTransformationFunc)
        {
            if ((webhookEvent?.Payload?.Object?.RecordingFiles ?? null) == null)
                throw new NullReferenceException();
            var requests = new List<(HttpRequestMessage requestMessage, string name)>();
            foreach (var item in webhookEvent.Payload.Object.RecordingFiles)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, item.DownloadUrl);
                if (!String.IsNullOrWhiteSpace(webhookEvent?.DownloadToken))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse($"Bearer ${webhookEvent.DownloadToken}");
                req.Headers.Add("Accept", "application/json");
                requests.Add((req, nameTransformationFunc(item)));
                req.Dispose();
            }
            return requests.ToArray();

        }
        private string ExampleNameTransformationFunc(RecordingFile recordingFile)
        {
            var sb = new StringBuilder();
            sb.Append(recordingFile.Id);
            sb.Append(recordingFile.RecordingStart?.ToLocalTime().ToString("MM_dd_yy__ss", CultureInfo.InvariantCulture));
            sb.Append("." + recordingFile.FileType.ToString());
            return sb.ToString();
        }
        internal async Task DownloadFileAsync(ZoomWebhookEvent webhookEvent)
        {
            var requests = GenerateZoomApiRequestsFromWebhook(webhookEvent, ExampleNameTransformationFunc);
            foreach (var (requestMessage, name) in requests)
            {
                await DownloadFileAsync(httpRequest: requestMessage, fileName: name);
                requestMessage.Dispose();
            }
        }
        protected async virtual Task<bool> IsFileLocked(IFileInfo file)
        {
            try
            {
                await using FileStream stream = File.Open( file.PhysicalPath,FileMode.Open, FileAccess.Read, FileShare.None);
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
        private async Task DownloadFileAsync(HttpRequestMessage httpRequest, string fileName, bool force = true)
        {
            try
            {
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                if (await IsFileLocked(fileInfo))
                    fileInfo = _fileProvider.GetFileInfo(fileName + "(copy)");

                if (fileInfo.Exists)
                {
                    if (!force)
                    {
                        _logger.LogError($"File already exists.  Set 'force' = true to override");
                        httpRequest.Dispose();
                        return;
                    }
                    else
                        _logger.LogInformation($"File exists at ${fileInfo.PhysicalPath}, overwriting");
                }
                using var client = _httpClientFactory.CreateClient();

                using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                await using var stream = await response.Content.ReadAsStreamAsync();

                await using var fs = File.Create(fileInfo.PhysicalPath);
                //stream.Seek(0, SeekOrigin.Begin);

                await stream.CopyToAsync(fs);

                _logger.LogInformation($"File saved as [{fileInfo.PhysicalPath}]");
                httpRequest.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError($"error during file download", e);
                throw;
            }



        }
        internal async Task<Stream> GetDownloadAsStreamAsync(HttpRequestMessage httpRequest)
        {
            using var client = _httpClientFactory.CreateClient();

            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug($"Sent request", httpRequest);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                _logger.LogError($"Error in download file request", response);
                throw new HttpRequestException($"Error in download file request, received ${response.StatusCode} in response to ${response.RequestMessage}");
            }
        }



        public void Dispose()
        {
            _fileProvider.Dispose();

        }
    }
}
