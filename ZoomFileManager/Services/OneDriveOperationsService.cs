using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            this._logger = logger;
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
}
