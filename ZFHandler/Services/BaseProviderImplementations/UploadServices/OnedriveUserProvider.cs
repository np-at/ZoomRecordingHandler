using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using ZFHandler.Helpers;
using ZFHandler.Models;
using ZFHandler.Models.ConfigurationSchemas;

namespace ZFHandler.Services.BaseProviderImplementations.UploadServices
{
    internal static class OnedriveHelpers
    {
        public static ClientSecretCredential DoAuth(BaseOneDriveClientConfig options) =>
            new(options.TenantId, options.ClientId, options.ClientSecret);
    }

    public class OnedriveUserProvider
    {
        private readonly ILogger<OnedriveUserProvider> _logger;
        private readonly Dictionary<string, UploadTargetConfig> _targetConfigs;
        private readonly Dictionary<string, GraphServiceClient> _clients = new();

        public OnedriveUserProvider(ILogger<OnedriveUserProvider> logger, IOptions<UploadTargetConfig[]> options)
        {
            _logger = logger;
            _targetConfigs = options.Value.Where(x => x.Type == JobType.OnedriveUser).ToDictionary(uploadTargetConfig =>
                uploadTargetConfig.Identifier ?? throw new NullReferenceException(uploadTargetConfig.Identifier));
        }

        private GraphServiceClient GetTargetClient(string name)
        {
            if (!_clients.ContainsKey(name))
                _clients.Add(name, new GraphServiceClient(OnedriveHelpers.DoAuth(
                    new OD_UserClientConfig(_targetConfigs[name]?.ClientConfig ??
                                          throw new IndexOutOfRangeException(
                                              $"Error creating GraphServiceClient: {name} not found in in target configs")))));
            return _clients[name];
        }

        private async Task<UploadResult<DriveItem>> UploadFileForProviderAsync(string providerName,
            IFileInfo sourceFileInfo,
            string? formattedRelativeItemPathWithName, CancellationToken token = default)
        {
            var gs = GetTargetClient(providerName);
            var targetConfig = _targetConfigs[providerName].ClientConfig;
            // var targetConfig = (OD_UserClientConfig?)_targetConfigs[providerName].ClientConfig;
            string targetName = targetConfig?.UserName ??
                                throw new NullReferenceException();
            // where you want to save the file, with name
            // you can use this to track exceptions, not used in this example

            var exceptions = new List<Exception>();

            for (int i = 0; i < 5; i++)
            {
                bool fileLocked = await FileHelpers.IsFileLocked(sourceFileInfo).ConfigureAwait(false);
                if (!fileLocked)
                    break;

                var d = new Random();
                int delay = 10000 + d.Next(10000);
                _logger.LogInformation("file {PhysicalPath} is in use, retrying in {DelaySeconds} seconds",
                    sourceFileInfo.PhysicalPath, delay / 1000);

                await Task.Delay(delay, token).ConfigureAwait(false);
            }

            await using var fileStream = sourceFileInfo.CreateReadStream();

            var uploadProps = new DriveItemUploadableProperties
            {
                ODataType = null,
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "rename" }
                }
            };
            string itemPath =
                $"/{targetConfig.RootDirectory}/{(string.IsNullOrWhiteSpace(formattedRelativeItemPathWithName) ? null : (formattedRelativeItemPathWithName.Trim('/') + '/'))}{sourceFileInfo.Name}";
            UploadSession uploadSession = await gs.Users[targetName].Drive.Root.ItemWithPath(itemPath)
                .CreateUploadSession(uploadProps).Request().PostAsync(token).ConfigureAwait(false);

            return await PerformUpload(uploadSession, fileStream, token).ConfigureAwait(false);
        }

        private async Task<UploadResult<DriveItem>> PerformUpload(IUploadSession uploadSession, Stream sourceFileStream,
            CancellationToken token = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var fileStreamLength = sourceFileStream.Length;
            long lastProgress = 0;
            // Max slice size must be a multiple of 320 KiB
            const int maxSliceSize = 320 * 1024 * 30;
            var fileUploadTask =
                new LargeFileUploadTask<DriveItem>(uploadSession, sourceFileStream, maxSliceSize);

            // Register the cancellation token to clean up the upload session if the operation is cancelled
            token.Register(() => fileUploadTask.DeleteSessionAsync().RunSynchronously());

            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> progress = new Progress<long>(progress2 =>
            {
                long spd = 0;
                try
                {
                    spd = (progress2 - lastProgress) / stopwatch.Elapsed.Milliseconds * 1000 / 1024 /
                          1024; // in MB/s
                }
                catch (DivideByZeroException)
                {
                    // ignore
                }

                Console.WriteLine(
                    $"[{100 * progress2 / fileStreamLength} % - {spd} mb/sec]Uploaded {progress2} bytes of {fileStreamLength} bytes");
                stopwatch.Restart();
                lastProgress = progress2;
            });

            try
            {
                // Upload the file
                var uploadResult = await fileUploadTask.UploadAsync(progress).ConfigureAwait(false);

                if (uploadResult.UploadSucceeded)
                    _logger.LogInformation("Upload of {Name}", uploadResult.ItemResponse.Name);
                else
                    _logger.LogError("Upload of item {Name} failed", uploadResult.ItemResponse.Name);

                // Console.WriteLine($"upload of {uploadResult.ItemResponse.Name} was successful");
                // var rHash = await WaitForRemoteHash(uploadResult.ItemResponse.Id)
                //     .ConfigureAwait(true);
                // if (CompareSha1(rHash))
                //     return 0;
                // else
                //     throw new CryptographicException("Hashes do not match");
                return uploadResult;
            }
            catch (ServiceException ex)
            {
                _logger.LogError("Error uploading: {Message}", ex.Message);
                throw;
            }
        }



        /// <summary>
        /// Given a source File to upload and a collection of targets,
        /// applies any applicable name formatting and starts the upload process
        /// </summary>
        /// <param name="uploadTargets"></param>
        /// <param name="sourceFileInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<UploadResult<DriveItem>> UploadForTargetsAsync(UploadTarget[] uploadTargets,
            IFileInfo sourceFileInfo,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var uploadTarget in uploadTargets)
            {
                var name = string.IsNullOrWhiteSpace(uploadTarget.NamingTemplate)
                    ? sourceFileInfo.Name
                    : string.Format(uploadTarget.NamingTemplate, sourceFileInfo.Name);

                var relativePath = uploadTarget.RelativeUploadPath;
                string itemPath =
                    $"/{relativePath}/{sourceFileInfo.Name}";


                yield return await UploadFileForProviderAsync(uploadTarget.ConfigId ?? "", sourceFileInfo,
                    uploadTarget.RelativeUploadPath, token);
            }
        }
    }
}