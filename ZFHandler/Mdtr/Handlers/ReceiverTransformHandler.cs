using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using ZFHandler.Controller;
using ZFHandler.Helpers;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Mdtr.Handlers
{
    public class ReceiverTransformHandler<T> : IRequestHandler<T, DownloadJobBatch> where T : IRConv<T>, new()
    {
        private readonly Func<T, CancellationToken?,ValueTask<IEnumerable<DownloadJob>>> _func;

        public ReceiverTransformHandler(IOptions<TransformerOption<T>> transformerOption)
        {
            _func = transformerOption?.Value?.TransformFunction ?? throw new NullReferenceException(nameof(transformerOption));
        }

        public async Task<DownloadJobBatch> Handle(T request, CancellationToken cancellationToken = default)
        {
            
            var jobs = await _func.Invoke(request, cancellationToken).ConfigureAwait(false);
            return new DownloadJobBatch
            {
                Jobs = jobs
            };
        }

        public async Task<DownloadJobBatch> Handle2(T request)
        {
            var jobs = await _func.Invoke(request, CancellationToken.None).ConfigureAwait(false);
            return new DownloadJobBatch
            {
                Jobs = jobs
            };
        }
    }
    
    
}