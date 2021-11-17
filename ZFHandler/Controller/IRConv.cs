using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Controller
{
    public interface IRConv<in T> : IRequest<DownloadJobBatch>
    {
        public Task<IEnumerable<DownloadJob>> ConvertToDownloadJobAsync(T input, CancellationToken ct = default);
    }
}