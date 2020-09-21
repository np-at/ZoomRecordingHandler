using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public class RecordingManagementService
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
                req.Headers.Authorization = AuthenticationHeaderValue.Parse($"Bearer ${webhookEvent.DownloadToken}");
                req.Headers.Add("Accept", "application/json");
                requests.Add((req, nameTransformationFunc(item)));
            }
            return requests.ToArray();

        }
        private string ExampleNameTransformationFunc(RecordingFile recordingFile)
        {
            var sb = new StringBuilder();
            sb.Append(recordingFile.Id);
            sb.Append(recordingFile.RecordingStart?.ToLocalTime().LocalDateTime.ToLongDateString());
            return sb.ToString();
        }
        internal async Task DownloadFileAsync(ZoomWebhookEvent webhookEvent)
        {
            var requests = GenerateZoomApiRequestsFromWebhook(webhookEvent, ExampleNameTransformationFunc);
            foreach (var (requestMessage, name) in requests)
            {
                await DownloadFileAsync(httpRequest: requestMessage, fileName: name);
            }
        }
        private async Task DownloadFileAsync(HttpRequestMessage httpRequest, string fileName, bool force = false)
        {
            try
            {
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                if (fileInfo.Exists)
                {
                    if (!force)
                    {
                        var exception = new Exception($"File already exists at ${fileInfo.PhysicalPath} and 'force' not specified");
                        _logger.LogError($"File already exists.  Set 'force' = true to override", exception);
                        throw exception;
                    }
                    else
                        _logger.LogInformation($"File exists at ${fileInfo.PhysicalPath}, overwriting");
                }

                await using var stream = await GetDownloadAsStreamAsync(httpRequest);
                await using var fs = File.Create(fileInfo.PhysicalPath);
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fs);

                _logger.LogInformation($"File saved as [{fileInfo.Name}]");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


        }
        internal async Task<Stream> GetDownloadAsStreamAsync(HttpRequestMessage httpRequest)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.SendAsync(httpRequest);
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
    }
}
