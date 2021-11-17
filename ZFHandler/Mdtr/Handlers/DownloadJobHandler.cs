using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Services;

namespace ZFHandler.Mdtr.Handlers
{
    public enum FileExistsBehavior
    {
        Unknown,
        Overwrite,
        Rename,
        Error
    }

    public class DownloadJobHandlerOptions
    {
        public string RootDirectory { get; set; }

        public FileExistsBehavior FileExistsBehavior { get; set; } = FileExistsBehavior.Overwrite;
    }

    public class DownloadJobHandler : IRequestHandler<DownloadJob, FileInfo?>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DownloadJobHandler> _logger;
        private readonly DownloadJobHandlerOptions _options;

        public DownloadJobHandler(ILogger<DownloadJobHandler> logger, IHttpClientFactory httpClientFactory,
            IOptions<DownloadJobHandlerOptions> options)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<FileInfo?> Handle(DownloadJob notification, CancellationToken cancellationToken)
        {
            JobTracker.DownloadJobs[notification.Id].Status = JobStatus.InProgress;
            // make sure the parent directory is there.  This should be a thread safe operation
            try
            {
                var directoryInfo = Directory.CreateDirectory(Path.Join(_options.RootDirectory ,notification.DestinationFolderPath));


                var destFileInfo = new FileInfo(Path.Join(directoryInfo.FullName, notification.DestinationFileName));
                var destinationFileInfo = new PhysicalFileInfo(destFileInfo);


                if (destFileInfo.Exists)
                {
                    switch (_options.FileExistsBehavior)
                    {
                        case FileExistsBehavior.Unknown:
                            throw new NotImplementedException();
                            break;
                        case FileExistsBehavior.Overwrite:
                            // do nothing
                            break;
                        case FileExistsBehavior.Rename:
                            throw new NotImplementedException();
                            break;
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
                        .SendAsync(notification.message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
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
                JobTracker.DownloadJobs[notification.Id].DestinationFile = destFileInfo;
                return destFileInfo;
            }
            catch (HttpRequestException e)
            {
                JobTracker.DownloadJobs[notification.Id].Status = JobStatus.Failed;
                _logger.LogError(e, "Error While attempting to start download");
                return null;
            }
            catch (Exception e)
            {
                JobTracker.DownloadJobs[notification.Id].Status = JobStatus.Failed;
                _logger.LogError(e,"Error while downloading {@File}  \n", notification.DestinationFileName);
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
    }
}