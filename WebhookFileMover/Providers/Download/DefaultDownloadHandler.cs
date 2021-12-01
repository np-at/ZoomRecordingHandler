using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Download
{
    public class DefaultDownloadHandler : IDownloadJobHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DefaultDownloadHandler> _logger;
        private readonly DownloadJobHandlerOptions _options;

        public DefaultDownloadHandler(ILogger<DefaultDownloadHandler> logger, IHttpClientFactory httpClientFactory,
            IOptions<DownloadJobHandlerOptions> options)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<FileInfo?> Handle(DownloadJob notification, CancellationToken cancellationToken)
        {
            // make sure the parent directory is there.  This should be a thread safe operation
            try
            {
                var directoryInfo =
                    Directory.CreateDirectory(Path.Join(_options.RootDirectory, notification.DestinationFolderPath));


                var destFileInfo = new FileInfo(Path.Join(directoryInfo.FullName, notification.DestinationFileName));
                var destinationFileInfo = new PhysicalFileInfo(destFileInfo);


                if (destFileInfo.Exists)
                {
                    switch (_options.FileExistsBehavior)
                    {
                        case FileExistsBehavior.Unknown:
                            throw new NotImplementedException();
                        case FileExistsBehavior.Overwrite:
                            // do nothing
                            break;
                        case FileExistsBehavior.Rename:
                            // TODO: Add destination file rename behavior support
                            throw new NotImplementedException();
                        case FileExistsBehavior.Error:
                            _logger.LogError(
                                "Specified file at {DestinationFileInfo} already exists.  Set 'failOnExists' = false to avoid this behavior",
                                destinationFileInfo.PhysicalPath);
                            throw new DuplicateNameException(
                                $"Specified file at {destinationFileInfo.PhysicalPath} already exists.  Set 'failOnExists' = false to avoid this behavior");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }


                ////////
                using var client = _httpClientFactory.CreateClient();

                using var response =
                    await client
                        .SendAsync(notification.Message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    response.EnsureSuccessStatusCode();


                //open incoming stream
                await using var stream =
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                //stream.Seek(0, SeekOrigin.Begin);

                //open dest file handle            
                await using var fs = File.Create(destinationFileInfo.PhysicalPath);
                // copy between incoming stream and file handle
                await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                await fs.FlushAsync(cancellationToken);
                _logger.LogDebug("File Downloaded and saved to {@FileInfo}", destinationFileInfo);

                return destFileInfo;
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error While attempting to start download");
                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while downloading {@File}  \n", notification.DestinationFileName);
                return null;
            }
        }

        private async Task<bool> IsFileLocked(string filePath)
        {
            try
            {
                var file = new FileInfo(filePath);

                await using FileStream stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read,
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
                _logger.LogError("failure: {Error}", ex);
                throw;
            }


            //file is not locked
            return false;
        }

        // private async Task<IFileInfo> DownloadFileAsync(HttpRequestMessage httpRequest, string fileName,
        //     string? relativePath,
        //     bool force = false, bool failOnExists = false)
        // {
        //     if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
        //     if (fileName == null)
        //         throw new ArgumentNullException(nameof(fileName));
        //     try
        //     {
        //         string? fullPath = Path.Join(relativePath, fileName);
        //         FileInfo fileInfo;
        //
        //
        //         try
        //         {
        //             Directory.CreateDirectory(Path.Join(_fileProvider.Root, relativePath));
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogError("Error creating directory: {Error}", e);
        //             throw;
        //         }
        //
        //         try
        //         {
        //             fileInfo = _fileProvider.GetFileInfo(fullPath);
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogError("error while getting file info: {Error}", e);
        //             throw;
        //         }
        //
        //         if (fileInfo.Exists)
        //         {
        //             if (failOnExists)
        //             {
        //                 _logger.LogError("File already exists.  Set 'failOnExists' = false to avoid this behavior");
        //                 // httpRequest.Dispose();
        //                 throw new IOException();
        //             }
        //
        //             int iterations = 0;
        //             bool fileLocked = !force || await IsFileLocked(fullPath).ConfigureAwait(false);
        //             while (fileLocked && iterations < 100)
        //             {
        //                 iterations++;
        //
        //                 int iterations1 = iterations;
        //                 string? testPath = _extensionRegex.Replace(fullPath,
        //                     match => $"({iterations1.ToString()})" + match.Value);
        //                 try
        //                 {
        //                     if (!_fileProvider.GetFileInfo(testPath).Exists)
        //                     {
        //                         fullPath = testPath;
        //                         break;
        //                     }
        //                 }
        //                 catch (Exception e)
        //                 {
        //                     Console.WriteLine(e);
        //                     throw;
        //                 }
        //
        //                 if (!force) continue;
        //                 fileLocked = await IsFileLocked(testPath).ConfigureAwait(false);
        //                 if (fileLocked) continue;
        //                 fullPath = testPath;
        //                 break;
        //             }
        //
        //             try
        //             {
        //                 fileInfo = _fileProvider.GetFileInfo(fullPath);
        //             }
        //             catch (Exception e)
        //             {
        //                 _logger.LogError("error checking file?: {Error}", e);
        //                 throw;
        //             }
        //         }
        //
        //         try
        //         {
        //             await using var fs = File.Create(fileInfo.PhysicalPath);
        //             using var client = _httpClientFactory.CreateClient();
        //             using var s = _httpClientFactory.CreateClient("test");
        //
        //             using var response =
        //                 await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)
        //                     .ConfigureAwait(false);
        //             if (!response.IsSuccessStatusCode)
        //                 response.EnsureSuccessStatusCode();
        //             await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        //             //stream.Seek(0, SeekOrigin.Begin);
        //
        //             await stream.CopyToAsync(fs).ConfigureAwait(false);
        //
        //             _logger.LogInformation("File saved as [{PhysicalPath}]", fileInfo.PhysicalPath);
        //             return fileInfo;
        //         }
        //         catch (IOException e)
        //         {
        //             _logger.LogError("IO ERROR: {E}", e.Message);
        //             throw;
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogError("misc error encountered while creating file: {Exception}", ex);
        //             throw;
        //         }
        //
        //         // await Task.Delay(1000);
        //
        //
        //         throw new IOException("Failed to create file after 5 attempts");
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError("error during file download", e);
        //         throw;
        //     }
        // }
        public async Task<IEnumerable<FileInfo?>> Handle(DownloadJobBatch input,
            CancellationToken cancellationToken = default)
        {
            List<FileInfo?> files = new();
            List<Exception> exceptions = new();
            if (input.Jobs == null) return files;

            foreach (var downloadJob in input.Jobs)
            {
                try
                {
                    var res = await this.Handle(downloadJob, cancellationToken);
                    files.Add(res);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error while downloading file: {0}", downloadJob.DestinationFileName);
                    exceptions.Add(e);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);

            return files;
        }

        public async Task<CompletedDownloadJob> HandleDownloadJobAsync(DownloadJob downloadJob,
            CancellationToken cancellationToken = default)
        {
            var fileInfo = await Handle(downloadJob, cancellationToken);
            if (fileInfo == null)
                throw new Exception();
            return downloadJob.CompleteDownloadJob(fileInfo);
        }
    }
}