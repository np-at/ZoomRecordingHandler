using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.Dropbox;
using ZoomFileManager.Helpers;

namespace ZoomFileManager.Services
{
    public interface IDropboxOperations
    {
        Task<FileMetadata> UploadAsync(string targetPath, FileInfo fileInfo,
            CancellationToken cancellationToken = default);
    }

    public class DropboxOperationsService : IDropboxOperations, IDisposable
    {
        private DropboxClient _dropboxClient;
        private readonly ILogger<DropboxOperationsService> _logger;

        public DropboxOperationsService(IHttpClientFactory httpClientFactory, IOptions<DropBoxClientConfig> dropboxOptions,
            ILogger<DropboxOperationsService> logger)
        {
            _logger = logger;
            _dropboxClient = new DropboxTeamClient(dropboxOptions.Value.RefreshToken, dropboxOptions.Value.ApiKey,
                dropboxOptions.Value.AppSecret, new DropboxClientConfig()
                {
                    // use named client which has extra long timeout configured
                    HttpClient = httpClientFactory.CreateClient("dropbox"),
                }).AsAdmin(dropboxOptions.Value.AdminTeamMemberId);
        }

        public async Task<FileMetadata> UploadAsync(string targetPath, FileInfo fileInfo,
            CancellationToken cancellationToken = default) =>
            await UploadFileAsync(fileInfo, targetPath, cancellationToken).ConfigureAwait(false);


        private async Task<FileMetadata> UploadFileAsync(FileInfo inputFile, string uploadPath,
            CancellationToken cancellationToken = default)
        {
            // chunks need to be multiple of 4194304 (except final piece) and no larger than 150mb
            const ulong chunkSize = (4194304 * 34);

            if (!inputFile.Exists)
                throw new FileNotFoundException("File provided for upload not found", inputFile.FullName);
            var account = await _dropboxClient.Users.GetCurrentAccountAsync();
            _dropboxClient = _dropboxClient.WithPathRoot(new PathRoot.Root(account.RootInfo.RootNamespaceId));
            var up = new UploadSessionType().AsConcurrent;
            var uploadSessionStartAsync =
                await _dropboxClient.Files.UploadSessionStartAsync(false, UploadSessionType.Concurrent.Instance,
                    Stream.Null);
            string sessionId = uploadSessionStartAsync.SessionId;
            await using var fileStream = new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read,
                4096, FileOptions.Asynchronous & FileOptions.SequentialScan);
            var fileSize = (ulong)fileStream.Length;
            var rangesRemaining = new List<Tuple<ulong, ulong>>();
            ulong cO = 0;
            while (true)
            {
                var newinc = fileSize - cO < chunkSize ? fileSize - cO : chunkSize;
                var newcO = cO + newinc;
                // ulong newcO = Math.Min(chunkSize, fileSize - cO) + cO;
                rangesRemaining.Add(new Tuple<ulong, ulong>(cO, newinc));
                cO = newcO;
                if (newinc != chunkSize)
                    break;
            }


            var uploadTasks = (from tuple in rangesRemaining.SkipLast(1)
                select UploadAppendAsync(fileStream, tuple.Item1, sessionId, chunkSize)).ToList();
            var c = Task.WhenAll(uploadTasks);
            try
            {
                await c.ConfigureAwait(false);
                await UploadAppendAsync(fileStream, rangesRemaining.Last().Item1, sessionId,
                    chunkSize, true);

                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                var fileMetadata = await _dropboxClient.Files.UploadSessionFinishAsync(
                    new UploadSessionCursor(sessionId, fileSize),
                    new CommitInfoWithProperties($"{(uploadPath == "/" ? null : uploadPath)}/{inputFile.Name}",
                        new WriteMode().AsOverwrite, true, DateTime.Now, true), Stream.Null);
                return fileMetadata;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// Convenience function to enable upload task batching
        /// </summary>
        /// <param name="parentStream"></param>
        /// <param name="offset"></param>
        /// <param name="sessionId"></param>
        /// <param name="chunkSize"></param>
        /// <param name="close"></param>
        private async Task UploadAppendAsync(Stream parentStream, ulong offset, string sessionId, ulong chunkSize,
            bool close = false)
        {
            try
            {
                Console.WriteLine("offset: {0}", offset);

                await using var requestBodyStream =
                    new ReadOnlySubStreamLocal(parentStream, (long)offset, (long)chunkSize);
                Console.WriteLine("req body length: {0}", requestBodyStream.Length);
                await _dropboxClient.Files.UploadSessionAppendV2Async(new UploadSessionCursor(sessionId, offset),
                    close, requestBodyStream).ConfigureAwait(false);
            }
            catch (ApiException<UploadSessionLookupError> e)
            {
                Console.WriteLine(e);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _dropboxClient.Dispose();
        }
    }
}