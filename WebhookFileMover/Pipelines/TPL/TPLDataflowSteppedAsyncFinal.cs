using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Models;

namespace WebhookFileMover.Pipelines.TPL
{
    public interface IImplementer<in T, TY> where T : notnull
    {
        Task<TY> Handle(T input, CancellationToken cancellationToken = default);
    }

    public class TPLFlowImplementation<T>
    {
        public delegate Action<FileInfo> FinishedEventHandler(object source, FileInfo fileInfo);

        public event FinishedEventHandler? Finished;

        private IServiceProvider _serviceProvider;
        private TPLDataflowSteppedAsyncFinal2<T, FileInfo> _pipeline;


        public TPLFlowImplementation(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pipeline = new TPLDataflowSteppedAsyncFinal2<T, FileInfo>();
        }

        public async Task RunAsync(T input)
        {
            await _pipeline.CreatePipeline(delegate(FileInfo info) { Finished?.Invoke(this, info); });

            _pipeline.Execute(input);
        }

        public void AddWebhookReceiver(Func<T, Task<DownloadJobBatch>> transformerFunc) =>
            _pipeline.AddStepAsync(transformerFunc);

        public void AddDownloadStep<TP>() where TP : IImplementer<DownloadJobBatch, IEnumerable<FileInfo>>
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetService<TP>();
            _pipeline.AddStepAsync<DownloadJobBatch, IEnumerable<FileInfo>>(async input =>
            {
                // var downloadJobs = input.Jobs as DownloadJob[] ?? input.Jobs?.ToArray() ?? throw new NullReferenceException();
                var files = new List<FileInfo>();
                var exceptions = new List<Exception>();
                try
                {
                    var file = await service?.Handle(input, CancellationToken.None)!;
                    var fileInfos = file as FileInfo[] ?? file.ToArray();
                    if (fileInfos?.Any() ?? false)
                        files.AddRange(fileInfos);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                    Console.WriteLine(e);
                }

                if (exceptions.Any())
                    throw new AggregateException(exceptions);
                return files;
            });
        }

        private void BuildFlow()
        {
        }
    }

    public class TPLDataflowSteppedAsyncFinal<TIn, TOut>
    {
        private List<(IDataflowBlock Block, bool IsAsync)> _steps = new();

        public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            if (_steps.Count == 0)
            {
                var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc);
                _steps.Add((step, IsAsync: false));
            }
            else
            {
                var lastStep = _steps.Last();
                if (!lastStep.IsAsync)
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc);
                    var targetBlock = (lastStep.Block as ISourceBlock<TLocalIn>);
                    targetBlock?.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add((step, IsAsync: false));
                }
                else
                {
                    var step = new TransformBlock<Task<TLocalIn>, TLocalOut>
                        (async (input) => stepFunc(await input));
                    var targetBlock = (lastStep.Block as ISourceBlock<Task<TLocalIn>>);
                    targetBlock?.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add((step, IsAsync: false));
                }
            }
        }

        public void AddStepAsync<TLocalIn, TLocalOut>
            (Func<TLocalIn, Task<TLocalOut>> stepFunc)
        {
            if (_steps.Count == 0)
            {
                var step = new TransformBlock<TLocalIn, Task<TLocalOut>>
                    (async (input) => await stepFunc(input));
                _steps.Add((step, IsAsync: true));
            }
            else
            {
                var lastStep = _steps.Last();
                if (lastStep.IsAsync)
                {
                    var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>
                        (async (input) => await stepFunc(await input));
                    var targetBlock = (lastStep.Block as ISourceBlock<Task<TLocalIn>>);
                    targetBlock?.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add((step, IsAsync: true));
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>
                        (async (input) => await stepFunc(input));
                    var targetBlock = (lastStep.Block as ISourceBlock<TLocalIn>);
                    targetBlock?.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add((step, IsAsync: true));
                }
            }
        }

        public async Task CreatePipeline(Action<TOut> resultCallback)
        {
            var lastStep = _steps.Last();
            if (lastStep.IsAsync)
            {
                var targetBlock = (lastStep.Block as ISourceBlock<Task<TOut>>);
                var callBackStep = new ActionBlock<Task<TOut>>
                    (async t => resultCallback(await t));
                targetBlock?.LinkTo(callBackStep);
            }
            else
            {
                var callBackStep = new ActionBlock<TOut>(resultCallback);
                var targetBlock = (lastStep.Block as ISourceBlock<TOut>);
                targetBlock?.LinkTo(callBackStep);
            }
        }

        public void Execute(TIn input)
        {
            var firstStep = _steps[0].Block as ITargetBlock<TIn>;
            firstStep?.SendAsync(input);
        }
    }
}