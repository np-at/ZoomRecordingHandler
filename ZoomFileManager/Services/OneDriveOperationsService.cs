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
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using File = System.IO.File;
using FileSystemInfo = System.IO.FileSystemInfo;
using ZoomFileManager.Helpers;
using static System.String;

namespace ZoomFileManager.Services
{
    public interface IOneDriveOperationsService
    {
        Task GetFilesAsync();

        Task PutFileAsync();

        Task DeleteFileAsync();
    }

    public class OneDriveOperationsService : IOneDriveOperationsService
    {
        private readonly ILogger<OneDriveOperationsService> _logger;


        public OneDriveOperationsService(ILogger<OneDriveOperationsService> logger)
        {
            _logger = logger;
        }

        public Task DeleteFileAsync()
        {
            throw new NotImplementedException();
        }

        public Task GetFilesAsync()
        {
            throw new NotImplementedException();
        }

        public Task PutFileAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class OdruOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? UserName { get; set; }

        public string? RootDirectory { get; set; }
    }

    public partial class Odru
    {
        private readonly ILogger<Odru> _logger;
        private readonly IOptions<OdruOptions> _options;
        private readonly string? _rootUploadPath;
        private readonly string _userName;
        private GraphServiceClient? _gs;

        public Odru(ILogger<Odru> logger, IOptions<OdruOptions> options)
        {
            this._logger = logger;
            _rootUploadPath = options.Value.RootDirectory;
            _userName = options.Value.UserName ?? throw new Exception();
            this._options = options;
            _gs = new GraphServiceClient(DoAuth(options.Value));
        }

        public async Task<UploadResult<DriveItem>> PutFileAsync(IFileInfo fileInfo, string? relativePath)
        {
            
            return await UploadTask(_userName, fileInfo, relativePath);
            
        }

        public async Task<DriveItem> GetParentItemAsync(DriveItem driveItem, CancellationToken ct)
        {
            if (_gs == null)
                throw new NullReferenceException(nameof(_gs));
            return await _gs.Drive.Items[driveItem.ParentReference.Id].Request().GetAsync(ct);
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
    }


    internal static class Extensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
        {
            return self.Select((item, index) => (item, index));
        }
    }

    public partial class Odru
    {
        private const int RecurseLevel = 1;

        internal async Task<User> GetUserAsync(string userName)
        {
            var userList = new List<User>();

            if (_gs == null)
                throw new Exception();
            var response = await _gs.Users
                .Request()
                .Filter($"userPrincipalName eq '{userName}'")
                .GetAsync();
            var pageIterator = PageIterator<User>
                .CreatePageIterator(_gs, response, u =>
                {
                    // Console.WriteLine(u.UserPrincipalName);
                    userList.Add(u);
                    return true;
                });
            await pageIterator.IterateAsync();
            if (userList.Count == 1)
                return userList.First();
            if (!userList.Any())
            {
                _logger.LogWarning($"Unable to find user matching {userName}");
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

        private async Task<List<DriveItem>> EnumerateFilesAsync(string user, string? rootDir,
            CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (_gs == null)
                throw new NullReferenceException();

            IDriveItemChildrenCollectionRequest drive;
            var collection = new List<DriveItem>();

            try
            {
                if (!IsNullOrWhiteSpace(rootDir))
                    drive = _gs.Users[user].Drive.Root.ItemWithPath(rootDir).Children
                        .Request();
                else
                    drive = _gs.Users[user].Drive.Root.Children.Request();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            IDriveItemChildrenCollectionPage? page;
            try
            {
                page = await drive.GetAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var startingItems = await GetDriveItemsFromPageAsync(page, cancellationToken);
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
                            var children = await _gs.Users[user].Drive.Items[child.Id].Children
                                .Request().GetAsync(cancellationToken);
                            var s = await GetDriveItemsFromPageAsync(children, cancellationToken);
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

        private async Task<IList<DriveItem>?> GetDriveItemsFromPageAsync(IDriveItemChildrenCollectionPage? page, CancellationToken cancellationToken)
        {
            if (page == null)
                return null;
            var collection = new List<DriveItem>();
            collection.AddRange(page.CurrentPage);
            while (page.NextPageRequest != null)
            {
                try
                {
                    page = await page.NextPageRequest.GetAsync(cancellationToken);
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

            var children = await GetDriveItemsFromPageAsync(driveItem.Children, cancellationToken);
            if (children == null)
                return null;

            foreach (var child in children)
            {
                var grandchild = await GetDriveItemsFromPageAsync(child.Children, cancellationToken);
                if (grandchild != null) collection.AddRange(grandchild);
            }

            return collection;
        }

        private async Task<UploadResult<DriveItem>> UploadTask(string user, IFileInfo filePath, string? relativePath)
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
                    var fileLocked =  await FileHelpers.IsFileLocked(filePath);
                    if (!fileLocked)
                        break;
                    else
                    {
                        var d = new Random();
                        var delay = 10000 + d.Next(10000);
                        _logger.LogInformation($"file {filePath.PhysicalPath} is in use, retrying in {delay/1000} seconds");

                        await Task.Delay(delay);
                    }
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
                        {"@microsoft.graph.conflictBehavior", "rename"}
                    }
                };
                // Create the upload session
                // itemPath does not need to be a path to an existing item
                var uploadSession = await _gs.Users[user].Drive.Root
                    .ItemWithPath(itemPath)
                    .CreateUploadSession(uploadProps)
                    .Request()
                    .PostAsync();

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
                        $"[{(100 * progress2 / fileStreamLength).ToString()} % - {spd} mb/sec]Uploaded {progress2.ToString()} bytes of {fileStreamLength.ToString()} bytes");
                    stopwatch.Restart();
                    lastProgress = progress2;
                });

                try
                {
                    // Upload the file
                    var uploadResult = await fileUploadTask.UploadAsync(progress);
                    

                    _logger.LogInformation(uploadResult.UploadSucceeded
                        ? $"Upload complete, item ID: {uploadResult.ItemResponse.Id}"
                        : "Upload failed");
                    Console.WriteLine($"upload of {uploadResult.ItemResponse.Name} was successful");
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
                    _logger.LogError($"Error uploading: {ex.Message}", ex);
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
                _logger.LogError($"Upload failed with exception {e.Message}", e);
                throw;
            }
        }

        private async Task<bool> CompareSha1(IFileInfo file, string uploadedHash)
        {
            using var sha = SHA1.Create();
            await using var fileStream = file.CreateReadStream();
            string localHash =
                Convert.ToBase64String(sha.ComputeHash(fileStream ?? throw new InvalidOperationException()));
            //debug
            Console.WriteLine($"remote sha1 = {uploadedHash}");
            Console.WriteLine($"local sha1 = {localHash}");

            return uploadedHash.Equals(localHash);
        }

        private byte[] GetFileBytes(FileSystemInfo filePath) => File.ReadAllBytes(filePath.FullName);

        private async Task<UploadSession> GetUploadSession(IGraphServiceClient client, string item, string user)
        {
            return await client.Users[user].Drive.Root.ItemWithPath(item).CreateUploadSession().Request().PostAsync();
        }

        private static ClientCredentialProvider DoAuth(OdruOptions options)
        {
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(options.ClientId)
                .WithTenantId(options.TenantId)
                .WithClientSecret(options.ClientSecret)
                .Build();

            var authProvider = new ClientCredentialProvider(confidentialClientApplication);
            return authProvider;
        }
    }
}