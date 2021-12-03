using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive;
using ZFHandler.Mdtr.Handlers;

namespace ZFHandler.Services.BaseProviderImplementations.UploadServices
{
    public abstract class BaseGraphClientProvider<T> : UploadJobHandler<T>
    {
        private readonly ILogger<BaseGraphClientProvider<T>> _logger;
        protected readonly GraphServiceClient _graphServiceClient;
        public BaseGraphClientProvider(ILogger<BaseGraphClientProvider<T>> logger, IOptions<BaseOneDriveClientConfig> oneDriveClientConfig)
        {
            _logger = logger;
            _graphServiceClient = oneDriveClientConfig.Value.CreateNewGraphServiceClientInstance();
        }

        internal abstract Task<IUploadSession> CreateUploadSession(IFileInfo sourceFileInfo, string? formattedRelativeItemPathWithName, CancellationToken cancellationToken = default);
        // internal async Task<UploadResult<DriveItem>> UploadFileForProviderAsync(string providerName,
        //     IFileInfo sourceFileInfo,
        //     string? formattedRelativeItemPathWithName, CancellationToken token = default)
        // {
        //     var gs = _gs;
        //     var targetConfig = _targetConfigs[providerName].ClientConfig;
        //     // var targetConfig = (OD_UserClientConfig?)_targetConfigs[providerName].ClientConfig;
        //     string targetName = targetConfig?.UserName ??
        //                         throw new NullReferenceException();
        //     // where you want to save the file, with name
        //     // you can use this to track exceptions, not used in this example
        //
        //     var exceptions = new List<Exception>();
        //
        //     for (int i = 0; i < 5; i++)
        //     {
        //         bool fileLocked = await FileHelpers.IsFileLocked(sourceFileInfo).ConfigureAwait(false);
        //         if (!fileLocked)
        //             break;
        //
        //         var d = new Random();
        //         int delay = 10000 + d.Next(10000);
        //         _logger.LogInformation("file {PhysicalPath} is in use, retrying in {DelaySeconds} seconds",
        //             sourceFileInfo.PhysicalPath, delay / 1000);
        //
        //         await Task.Delay(delay, token).ConfigureAwait(false);
        //     }
        //
        //     await using var fileStream = sourceFileInfo.CreateReadStream();
        //
        //     var uploadProps = new DriveItemUploadableProperties
        //     {
        //         ODataType = null,
        //         AdditionalData = new Dictionary<string, object>
        //         {
        //             { "@microsoft.graph.conflictBehavior", "rename" }
        //         }
        //     };
        //     string itemPath =
        //         $"/{targetConfig.RootDirectory}/{(string.IsNullOrWhiteSpace(formattedRelativeItemPathWithName) ? null : (formattedRelativeItemPathWithName.Trim('/') + '/'))}{sourceFileInfo.Name}";
        //     UploadSession uploadSession = await gs.Users[targetName].Drive.Root.ItemWithPath(itemPath)
        //         .CreateUploadSession(uploadProps).Request().PostAsync(token).ConfigureAwait(false);
        //
        //     return await PerformUpload(uploadSession, fileStream, token).ConfigureAwait(false);
        // }

        internal async Task<UploadResult<DriveItem>> PerformUpload(IUploadSession uploadSession, Stream sourceFileStream,
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


    }
}