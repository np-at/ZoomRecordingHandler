using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using ZFHandler.CustomBuilders;
using ZFHandler.Mdtr.Handlers;
using ZFHandler.Models;

namespace ZFHandler.Services.BaseProviderImplementations.UploadServices
{
    public class SharepointProvider<T> : BaseGraphClientProvider<T>
    {
        private readonly ILogger<SharepointProvider<T>> _logger;
        private readonly SharepointProviderConfig<T> _options;

        public SharepointProvider(ILogger<SharepointProvider<T>> logger, IOptions<SharepointProviderConfig<T>> options) : base(logger, options)
        {
            _logger = logger;
            _options = options.Value;
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