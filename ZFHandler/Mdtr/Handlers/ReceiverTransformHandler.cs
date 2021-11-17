using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ZFHandler.Controller;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Mdtr.Handlers
{
    public class ReceiverTransformHandler<T> : IRequestHandler<T, DownloadJobBatch> where T : IRConv<T>, new()
    {
        private readonly T _conv;

        public ReceiverTransformHandler()
        {
            _conv = new T();
        }

        public async Task<DownloadJobBatch> Handle(T request, CancellationToken cancellationToken)
        {
            var jobs = await _conv.ConvertToDownloadJobAsync(request, cancellationToken).ConfigureAwait(false);
            return new DownloadJobBatch
            {
                Jobs = jobs
            };
        }
    }
    
    
}