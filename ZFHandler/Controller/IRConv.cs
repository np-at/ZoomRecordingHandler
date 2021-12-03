using MediatR;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Controller
{
    public interface IRConv<in T> : IRequest<DownloadJobBatch>
    {
    }
}