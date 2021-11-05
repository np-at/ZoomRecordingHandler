using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Identity.Client;
using File = System.IO.File;
using FileSystemInfo = System.IO.FileSystemInfo;
using ZoomFileManager.Helpers;
using static System.String;
using Microsoft.Graph.Extensions;
using ZoomFileManager.Models.ConfigurationSchemas;

namespace ZoomFileManager.Services
{
    internal static class Extensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
        {
            return self.Select((item, index) => (item, index));
        }
    }

    public class OneDriveOperationsService : IUploadService
    {
        private const int RecurseLevel = 1;
        
        public OneDriveOperationsService(ILogger<OneDriveOperationsService> logger,
            IOptions<SharepointClientConfig> options)
        {
            _logger = logger;
            _options = options;
            _rootUploadPath = options.Value.RootDirectory;
            _targetType = TargetType.Site;
            _targetName = options.Value?.SiteName ?? throw new NullReferenceException("SiteName Not Provided for Sharepoint Configuration");

        }

        public OneDriveOperationsService(ILogger<OneDriveOperationsService> logger,
            IOptions<OD_DriveClientConfig> options)
        {
            _logger = logger;
            _options = options;
            _rootUploadPath = options.Value.RootDirectory;
            _targetType = TargetType.Drive;
            _targetName = options.Value?.DriveId ??
                          throw new NullReferenceException(
                              "Drive Id not provided for Onedrive Drive Client Configuration");

        }

        public OneDriveOperationsService(ILogger<OneDriveOperationsService> logger, IOptions<OD_UserClientConfig> options)
        {
            _logger = logger;
            _options = options;
            _rootUploadPath = options.Value.RootDirectory;
            _targetType = TargetType.User;
            _targetName = options.Value?.UserName ??     throw new NullReferenceException(
                "Username not provided for Onedrive User Client Configuration");
        }
        // public OneDriveOperationsService(ILogger<OneDriveOperationsService> logger, IOptions<BaseOneDriveClientConfig> options)
        // {
        //     this._logger = logger;
        //     _rootUploadPath = options.Value.RootDirectory;
        //     
        //     int s = (2 * Convert.ToByte(!IsNullOrWhiteSpace(options.Value.UserName))) +
        //             (3 * Convert.ToByte(!IsNullOrWhiteSpace(options.Value.SiteName))) +
        //             (4 * Convert.ToByte(!IsNullOrWhiteSpace(options.Value.DriveId)));
        //     _targetType = s switch
        //     {
        //         2 => TargetType.User,
        //         3 => TargetType.Site,
        //         4 => TargetType.Drive,
        //         _ => throw new ArgumentException("Must provide exactly one of UserName or SiteName or DriveId")
        //     };
        //     _targetName = _targetType switch
        //     {
        //         TargetType.Drive => options.Value.DriveId,
        //         TargetType.Site => options.Value.SiteName,
        //         TargetType.User => options.Value.UserName,
        //         _ => throw new ArgumentOutOfRangeException()
        //     } ?? throw new InvalidOperationException();
        //     this._options = options;
        //     _gs = new GraphServiceClient(DoAuth(options.Value));
        // }


        private static async Task<IList<DriveItem>?> GetDriveItemsFromPageAsync(IDriveItemChildrenCollectionPage? page,
            CancellationToken cancellationToken)
        {
            if (page == null)
                return null;
            var collection = new List<DriveItem>();
            collection.AddRange(page.CurrentPage);
            while (page.NextPageRequest != null)
            {
                try
                {
                    page = await page.NextPageRequest.GetAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }

                Console.WriteLine(page.CurrentPage);
                collection.AddRange(page.CurrentPage);
                Console.WriteLine(collection);
            }

            return collection;
        }

        internal async Task<IList<DriveItem>?> GetDriveItemChildrenAsync(DriveItem driveItem,
            CancellationToken cancellationToken)
        {
            var collection = new List<DriveItem>();

            var children = await GetDriveItemsFromPageAsync(driveItem.Children, cancellationToken).ConfigureAwait(false);
            if (children == null)
                return null;

            foreach (var child in children)
            {
                var grandchild = await GetDriveItemsFromPageAsync(child.Children, cancellationToken).ConfigureAwait(false);
                if (grandchild != null) collection.AddRange(grandchild);
            }

            return collection;
        }

        private async Task<UploadResult<DriveItem>> UploadTask(string uploadTarget, IFileInfo filePath, string? relativePath,
            TargetType targetType)
        {
            if (_gs == null)
                throw new NullReferenceException(nameof(_gs));

            try
            {
                // where you want to save the file, with name
                string itemPath =
                    $"/{_rootUploadPath}/{(IsNullOrWhiteSpace(relativePath) ? null : (relativePath.Trim('/') + '/'))}{filePath.Name}";


                // you can use this to track exceptions, not used in this example
                var exceptions = new List<Exception>();

                for (int i = 0; i < 5; i++)
                {
                    bool fileLocked = await FileHelpers.IsFileLocked(filePath).ConfigureAwait(false);
                    if (!fileLocked)
                        break;

                    var d = new Random();
                    int delay = 10000 + d.Next(10000);
                    _logger.LogInformation("file {PhysicalPath} is in use, retrying in {DelaySeconds} seconds",
                        filePath.PhysicalPath, delay / 1000);

                    await Task.Delay(delay).ConfigureAwait(false);
                }

                await using var fileStream = filePath.CreateReadStream();

                // avoid dereferencing disposed var later
                long fileStreamLength = fileStream.Length;

                var stopwatch = Stopwatch.StartNew();
                long lastProgress = 0;
                var uploadProps = new DriveItemUploadableProperties
                {
                    ODataType = null,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "rename" }
                    }
                };


                // Create the upload session
                // itemPath does not need to be a path to an existing item
                UploadSession uploadSession = targetType switch
                {
                    TargetType.Drive => await _gs.Drives[uploadTarget].Root
                        .ItemWithPath(itemPath)
                        .CreateUploadSession(uploadProps)
                        .Request()
                        .PostAsync()
                        .ConfigureAwait(false),
                    TargetType.Site => await _gs.Sites[uploadTarget].Drive.Root
                        .ItemWithPath(itemPath)
                        .CreateUploadSession(uploadProps)
                        .Request()
                        .PostAsync()
                        .ConfigureAwait(false),
                    TargetType.User => await _gs.Users[uploadTarget].Drive.Root
                        .ItemWithPath(itemPath)
                        .CreateUploadSession(uploadProps)
                        .Request()
                        .PostAsync()
                        .ConfigureAwait(false),
                    _ => throw new ArgumentOutOfRangeException(nameof(targetType), targetType, null)
                };



                // Max slice size must be a multiple of 320 KiB
                const int maxSliceSize = 320 * 1024 * 30;
                var fileUploadTask =
                    new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize);

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


                // // upload the chunks
                // foreach (var (chunk, i) in chunks.WithIndex())
                // {
                //     var chunkRequestResponse = await uploadProvider.GetChunkRequestResponseAsync(chunk, exceptions);
                //
                //     Console.WriteLine($"Uploading chunk {i} out of {chunks.Count()}");
                //
                //     // when the chunks are finished...
                //     if (!chunkRequestResponse.UploadSucceeded) continue;
                //     Console.WriteLine("Upload is complete", chunkRequestResponse.ItemResponse);
                //     return 0;
                // }
            }
            catch (Exception e)
            {
                _logger.LogError("Upload failed with exception {Message}", e.Message);
                throw;
            }
        }

        private async Task<bool> CompareSha1(IFileInfo file, string uploadedHash)
        {
            using var sha = SHA1.Create();
            await using var fileStream = file.CreateReadStream();
            string localHash =
                Convert.ToBase64String(await sha.ComputeHashAsync(fileStream ?? throw new InvalidOperationException()).ConfigureAwait(false));
            //debug
            Console.WriteLine($"remote sha1 = {uploadedHash}");
            Console.WriteLine($"local sha1 = {localHash}");

            return uploadedHash.Equals(localHash);
        }

        private byte[] GetFileBytes(FileSystemInfo filePath) => File.ReadAllBytes(filePath.FullName);

        
        private async Task<UploadSession> GetUploadSession(GraphServiceClient client, string item, string user)
        {
            return await client.Users[user].Drive.Root.ItemWithPath(item).CreateUploadSession().Request().PostAsync().ConfigureAwait(false);
        }

        private static ClientSecretCredential DoAuth(BaseOneDriveClientConfig options) => new(options.TenantId, options.ClientId, options.ClientSecret);

        private readonly ILogger<OneDriveOperationsService> _logger;
        private readonly IOptions<BaseOneDriveClientConfig> _options;
        private readonly string? _rootUploadPath;
        private readonly string _targetName;
        private readonly GraphServiceClient? _gs;
        private readonly TargetType _targetType;

        private enum TargetType
        {
            Drive,
            Site,
            User
        }



    

        public async Task<DriveItem> GetParentItemAsync(DriveItem driveItem, CancellationToken ct)
        {
            if (_gs == null)
                throw new NullReferenceException(nameof(_gs));
            return await _gs.Drive.Items[driveItem.ParentReference.Id].Request().GetAsync(ct).ConfigureAwait(false);
        }
        internal async Task<User> GetUserAsync(string userName)
        {
            var userList = new List<User>();

            if (_gs == null)
                throw new Exception();
            var response = await _gs.Users
                .Request()
                .Filter($"userPrincipalName eq '{userName}'")
                .GetAsync()
                .ConfigureAwait(false);
            var pageIterator = PageIterator<User>
                .CreatePageIterator(_gs, response, u =>
                {
                    // Console.WriteLine(u.UserPrincipalName);
                    userList.Add(u);
                    return true;
                });
            await pageIterator.IterateAsync().ConfigureAwait(false);
            if (userList.Count == 1)
                return userList[0];
            if (!userList.Any())
            {
                _logger.LogWarning("Unable to find uploadTarget matching {UserName}", userName);
                throw new ArgumentException();
            }

            while (true)
            {
                Console.WriteLine("Found multiple possible entries, please select");
                foreach (var user in userList)
                    Console.WriteLine($"{userList.IndexOf(user).ToString()}. {user.UserPrincipalName}");

                string? selection = Console.ReadLine();
                if (!int.TryParse(selection, out int selectionIndex)) continue;
                if (userList[selectionIndex] != null)
                    return userList[selectionIndex];
            }
        }

        private async Task<List<DriveItem>> EnumerateFilesAsync(string target, string? rootDir,
            CancellationToken cancellationToken)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (_gs == null)
                throw new NullReferenceException();

            IDriveItemChildrenCollectionRequest drive;
            var collection = new List<DriveItem>();

            try
            {
                drive = _targetType switch
                {
                    TargetType.Drive => _gs.Drives[target].Root.ItemWithPath(rootDir).Children.Request(),
                    TargetType.Site => _gs.Sites[target].Drive.Root.ItemWithPath(rootDir).Children
                        .Request(),
                    TargetType.User => _gs.Users[target].Drive.Root.ItemWithPath(rootDir).Children
                        .Request(),
                    _ => throw new ArgumentOutOfRangeException()
                };

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            IDriveItemChildrenCollectionPage? page;
            try
            {
                page = await drive.GetAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var startingItems = await GetDriveItemsFromPageAsync(page, cancellationToken).ConfigureAwait(false);
            if (startingItems == null)
                throw new NullReferenceException();
            List<DriveItem> currentLevelChildren = startingItems.ToList();
            try
            {
                for (int i = 0; i < RecurseLevel; i++)
                {
                    var nextLevelChildren = new List<DriveItem>();
                    foreach (var child in currentLevelChildren)
                        try
                        {
                            var children = await _gs.Users[target].Drive.Items[child.Id].Children
                                .Request().GetAsync(cancellationToken).ConfigureAwait(false);
                            var s = await GetDriveItemsFromPageAsync(children, cancellationToken).ConfigureAwait(false);
                            if (s != null)
                                nextLevelChildren.AddRange(s);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }

                    // after collecting all the children of the current batch, add them to the results pile and shift collections up one level
                    collection.AddRange(currentLevelChildren);

                    // if this is the last round, items in nextLevelChildren would not have a chance to be added to results pile, so add them now
                    if (i == RecurseLevel - 1)
                        collection.AddRange(nextLevelChildren);
                    else
                        currentLevelChildren = nextLevelChildren.ToList();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return collection;
        }
        #region RefCode

        // private static async Task<string> WaitForRemoteHash(string remoteFileId, int maxWaitIntervals = 10)
        // {
        //     string? remoteHash = null;
        //     int i = 0;
        //     while (string.IsNullOrWhiteSpace(remoteHash))
        //     {
        //         if (i > maxWaitIntervals)
        //             throw new TimeoutException();
        //         Console.WriteLine("Waiting 5 seconds for remote hash to be computed");
        //         Task.Delay(5000).Wait();
        //         
        //         var remoteItem = await _gs.Users[_user.Id].Drive.Items[remoteFileId].Request().GetAsync();
        //         remoteHash = remoteItem.File.Hashes.Sha1Hash;
        //         i++;
        //         
        //     }
        //
        //     return remoteHash ?? "";
        // }

        // static GraphServiceClient GetGraphServiceClient(string token)
        // {
        //     return new GraphServiceClient(
        //         new DelegateAuthenticationProvider(
        //             (requestMessage) =>
        //             {
        //                 requestMessage.Headers.Authorization =
        //                     new AuthenticationHeaderValue("Bearer", token);
        //
        //                 return Task.FromResult(0);
        //             }));
        // }

        #endregion

        public async Task GetFilesAsync()
        {
            throw new NotImplementedException();
        }
        public async Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo fileInfo, string? relativePath)
        {
            return await UploadTask(_targetName, fileInfo, relativePath, _targetType).ConfigureAwait(false);
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(string uploadTarget, IFileInfo filePath, string? relativePath)
        {
            throw new NotImplementedException();
        }


        public async Task DeleteFileAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo sourceFileInfo, string? relativePath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}