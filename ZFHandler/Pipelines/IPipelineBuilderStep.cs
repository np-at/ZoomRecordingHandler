using System;

namespace ZFHandler.Pipelines
{
    public interface IPipelineBuilderStep<in TIn, TOut>
    {
        IPipelineBuilderStep<TIn, TNewStepOut> AddStep<TNewStepOut>(Func<TOut, TNewStepOut> stepFunc, int workerCount);
        IPipeline<TIn, TOut> Create();
    }
}