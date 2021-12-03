using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using ZFHandler.CustomBuilders;
using ZFHandler.Models;

namespace ZFHandler.Services.BaseProviderImplementations.UploadServices
{
    public class OnedriveDriveProvider<T> : BaseGraphClientProvider<T>
    {
        private readonly OnedriveDriveProviderOptions<T> _options;
        private readonly ILogger<OnedriveDriveProvider<T>> _logger;

        public OnedriveDriveProvider(IOptions<OnedriveDriveProviderOptions<T>> options, ILogger<OnedriveDriveProvider<T>> logger) : base(logger, options)
        {
            _options = options.Value;
            _logger = logger;
        }

        public override async Task Handle(UploadJobSpec<T> notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override async Task<IUploadSession> CreateUploadSession(IFileInfo sourceFileInfo, string? formattedRelativeItemPathWithName,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}