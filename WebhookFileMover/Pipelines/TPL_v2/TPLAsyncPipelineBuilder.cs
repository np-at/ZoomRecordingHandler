﻿using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WebhookFileMover.Pipelines.TPL_v2
{
    public sealed class TplAsyncPipelineBuilder<TInput, TOutput> :
        TplAsyncPipelineBuilder<TInput, TInput, TOutput>
    {
        public TplAsyncPipelineBuilder()
        {
        }
    }

    public class TplAsyncPipelineBuilder<TFirstInput, TCurrentInput, TLastOutput>
    {
        private readonly IDataflowBlock _firstStep;

        private readonly IDataflowBlock _lastStep;

        private readonly TaskCompletionSource<TLastOutput> _taskCompletionSource;


        public TplAsyncPipelineBuilder()
            : this(null, null, new TaskCompletionSource<TLastOutput>())
        {
        }

        private TplAsyncPipelineBuilder(IDataflowBlock firstStep, IDataflowBlock lastStep,
            TaskCompletionSource<TLastOutput> taskCompletionSource)
        {
            _firstStep = firstStep;
            _lastStep = lastStep;
            _taskCompletionSource = taskCompletionSource ?? throw new ArgumentNullException(nameof(taskCompletionSource));
        }

        public TplAsyncPipelineBuilder<TFirstInput, TOutput, TLastOutput> AddStep<TOutput>(
            Func<TCurrentInput, Task<TOutput>> transformAsync)
        {
            IDataflowBlock lastStep = CreateBlockFromLastStep(transformAsync);

            IDataflowBlock firstStep = _firstStep ?? lastStep;

            return new TplAsyncPipelineBuilder<TFirstInput, TOutput, TLastOutput>(
                firstStep, lastStep, _taskCompletionSource
            );
        }

        public TplAsyncPipelineBuilder<TFirstInput, TOutput, TLastOutput> AddStep<TOutput>(
            Func<TCurrentInput, TOutput> transformSync)
        {
            IDataflowBlock lastStep = CreateBlockFromLastStep(transformSync);

            IDataflowBlock firstStep = _firstStep ?? lastStep;

            return new TplAsyncPipelineBuilder<TFirstInput, TOutput, TLastOutput>(
                firstStep, lastStep, _taskCompletionSource
            );
        }

        public TplAsyncPipeline<TFirstInput, TLastOutput> Build()
        {
            if (!(_firstStep is ITargetBlock<TFirstInput> firstStep))
            {
                throw new InvalidOperationException("The first step block is not initialized.");
            }

            AddLastStep(input => _taskCompletionSource.SetResult(input));

            return new TplAsyncPipeline<TFirstInput, TLastOutput>(
                firstStep, _taskCompletionSource.Task
            );
        }

        private TplAsyncPipelineBuilder<TFirstInput, TLastOutput, TLastOutput> AddLastStep(
            Action<TLastOutput> callback)
        {
            if (!(_lastStep is ISourceBlock<TLastOutput> setResultBlock))
            {
                return AddLastAsyncStep(callback);
            }

            var setResultStep = new ActionBlock<TLastOutput>(
                input => callback(input)
            );

            setResultBlock.LinkTo(setResultStep);

            IDataflowBlock firstStep = _firstStep ?? setResultStep;

            return new TplAsyncPipelineBuilder<TFirstInput, TLastOutput, TLastOutput>(
                firstStep, setResultStep, _taskCompletionSource
            );
        }

        private TplAsyncPipelineBuilder<TFirstInput, TLastOutput, TLastOutput> AddLastAsyncStep(
            Action<TLastOutput> callback)
        {
            if (!(_lastStep is ISourceBlock<Task<TLastOutput>> setResultBlock))
            {
                throw new InvalidOperationException(
                    "Cannot cast the last block to set up final callback."
                );
            }

            var setResultStep = new ActionBlock<Task<TLastOutput>>(
                async input => callback(await input)
            );

            setResultBlock.LinkTo(setResultStep);

            IDataflowBlock firstStep = _firstStep ?? setResultStep;

            return new TplAsyncPipelineBuilder<TFirstInput, TLastOutput, TLastOutput>(
                firstStep, setResultStep, _taskCompletionSource
            );
        }

        private IDataflowBlock CreateBlockFromLastStep<TOutput>(Func<TCurrentInput, TOutput> transformSync)
        {
            switch (_lastStep)
            {
                case null:
                    return new TransformBlock<TCurrentInput, TOutput>(
                        input => FuncHelper.WrapFuncWithCatch(transformSync, input, _taskCompletionSource)
                    );

                case ISourceBlock<Task<TCurrentInput>> asyncSourceBlock:
                    return asyncSourceBlock.LinkAsyncBlockWithSync(transformSync, _taskCompletionSource);

                case ISourceBlock<TCurrentInput> sourceBlock:
                    return sourceBlock.LinkSyncBlockWithSync(transformSync, _taskCompletionSource);

                default:
                    throw new InvalidOperationException("Cannot find proper matching for the last step block.");
            };
        }

        private IDataflowBlock CreateBlockFromLastStep<TOutput>(Func<TCurrentInput, Task<TOutput>> transformAsync)
        {
            switch (_lastStep)
            {
                case null:
                    return new TransformBlock<TCurrentInput, Task<TOutput>>(
                        input => FuncHelper.WrapFuncWithCatch(transformAsync, input, _taskCompletionSource)
                    );

                case ISourceBlock<Task<TCurrentInput>> asyncSourceBlock:
                    return asyncSourceBlock.LinkAsyncBlockWithAsync(transformAsync, _taskCompletionSource);

                case ISourceBlock<TCurrentInput> sourceBlock:
                    return sourceBlock.LinkSyncBlockWithAsync(transformAsync, _taskCompletionSource);

                default:
                    throw new InvalidOperationException("Cannot find proper matching for the last step block.");
            };
        }
    }
}