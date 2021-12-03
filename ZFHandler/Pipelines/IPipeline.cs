using System;
using System.Threading.Tasks;

namespace ZFHandler.Pipelines
{
    public interface IPipeline<in TIn, TOut> : IDisposable
    {
        // TODO: use ValueTask + IValueTaskSource to avoid allocations
        Task<TOut> Execute(TIn data);
    }
}