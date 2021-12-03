using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookFileMover.Pipelines
{
    public interface IPipeline
    {
        void Execute(object input);
        event Action<object> Finished;
    }

    public interface IPipelineStep<TStepIn>
    {
        BlockingCollection<TStepIn> Buffer { get; set; }
    }

    public class GenericBCPipelineStep<TStepIn, TStepOut> : IPipelineStep<TStepIn>
    {
        public BlockingCollection<TStepIn> Buffer { get; set; } = new();
        public Func<TStepIn, TStepOut>? StepAction { get; set; }
    }

    public static class GenericBCPipelineExtensions
    {
        public static TOutput Step<TInput, TOutput, TInputOuter, TOutputOuter>
        (this TInput inputType,
            GenericBCPipeline<TInputOuter, TOutputOuter> pipelineBuilder,
            Func<TInput, TOutput> step)
        {
            var pipelineStep = pipelineBuilder.GenerateStep<TInput, TOutput>();
            pipelineStep.StepAction = step;
            return default(TOutput) ?? throw new InvalidOperationException();
        }
    }

    public class GenericBCPipeline<TPipeIn, TPipeOut>
    {
        private List<object> _pipelineSteps = new();
        public event Action<TPipeOut>? Finished;

        public GenericBCPipeline(Func<TPipeIn, GenericBCPipeline<TPipeIn, TPipeOut>, TPipeOut> steps)
        {
            steps.Invoke(default(TPipeIn), this); // Invoke just once to buld blocking collections
        }

        public void Execute(TPipeIn input)
        {
            var first = _pipelineSteps[0] as IPipelineStep<TPipeIn>;
            first?.Buffer.Add(input);
        }

        public GenericBCPipelineStep<TStepIn, TStepOut> GenerateStep<TStepIn, TStepOut>()
        {
            var pipelineStep = new GenericBCPipelineStep<TStepIn, TStepOut>();
            var stepIndex = _pipelineSteps.Count;

            Task.Run((() =>
            {
                IPipelineStep<TStepOut>? nextPipelineStep = null;
                foreach (var input in pipelineStep.Buffer.GetConsumingEnumerable())
                {
                    bool isLastStep = stepIndex == _pipelineSteps.Count - 1;
                    var output = pipelineStep.StepAction(input);
                    if (isLastStep)
                    {
                        // This is dangerous as the invocation is added to the last step
                        // Alternatively, you can utilize BeginInvoke like here: https://stackoverflow.com/a/16336361/1229063
                        Finished?.Invoke((TPipeOut)(object)output ?? throw new InvalidOperationException());
                    }
                    else
                    {
                        nextPipelineStep ??= (isLastStep
                            ? null
                            : _pipelineSteps[stepIndex + 1] as IPipelineStep<TStepOut>);
                        nextPipelineStep?.Buffer.Add(output);
                    }
                }
            }));
            _pipelineSteps.Add(pipelineStep);
            return pipelineStep;
        }
    }

    public class CastingPipelineWithParallelism : IPipeline
    {
        class Step
        {
            public Func<object, object> Func { get; set; }
            public int DegreeOfParallelism { get; set; }
            public int MaxCapacity { get; set; } // !!!
        }

        List<Step> _pipelineSteps = new List<Step>();
        BlockingCollection<object>[] _buffers;

        public event Action<object> Finished;

        public void AddStep(Func<object, object> stepFunc, int degreeOfParallelism, int maxCapacity)
        {
            // !!! Save the degree of parallelism
            _pipelineSteps.Add(new Step()
            {
                Func = stepFunc,
                DegreeOfParallelism = degreeOfParallelism,
                MaxCapacity = maxCapacity // !!!
            });
        }

        public void Execute(object input)
        {
            var first = _buffers[0];
            first.Add(input);
        }

        public IPipeline GetPipeline()
        {
            _buffers = _pipelineSteps.Select(step => new BlockingCollection<object>(step.MaxCapacity)).ToArray();

            int bufferIndex = 0;
            foreach (var pipelineStep in _pipelineSteps)
            {
                var bufferIndexLocal = bufferIndex;

                // !!! start as many threads as there are degrees of parallelism.
                for (int i = 0; i < pipelineStep.DegreeOfParallelism; i++)
                {
                    Task.Run(() => { StartStep(bufferIndexLocal, pipelineStep); });
                }

                bufferIndex++;
            }

            return this;
        }

        private void StartStep(int bufferIndexLocal, Step pipelineStep)
        {
            foreach (var input in _buffers[bufferIndexLocal].GetConsumingEnumerable())
            {
                var output = pipelineStep.Func.Invoke(input);
                bool isLastStep = bufferIndexLocal == _pipelineSteps.Count - 1;
                if (isLastStep)
                {
                    Finished?.Invoke(output);
                }
                else
                {
                    var next = _buffers[bufferIndexLocal + 1];
                    next.Add(output);
                }
            }
        }
    }
}