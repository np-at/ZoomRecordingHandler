namespace ZFHandler.Services.BaseProviderImplementations.DownloadServices
{
    public partial class ZoomDownloadService
    {
        
        
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
        //         IFileInfo fileInfo;
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
        //                 await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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
        // private async Task<bool> IsFileLocked(string filePath)
        // {
        //     try
        //     {
        //         var file = _fileProvider.GetFileInfo(filePath);
        //         await using FileStream stream = File.Open(file.PhysicalPath, FileMode.Open, FileAccess.Read,
        //             FileShare.None);
        //     }
        //     catch (IOException)
        //     {
        //         //the file is unavailable because it is:
        //         //still being written to
        //         //or being processed by another thread
        //         //or does not exist (has already been processed)
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError("failure: {Error}", ex);
        //         throw;
        //     }
        //
        //
        //     //file is not locked
        //     return false;
        // }
    }
}