using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZFHandler.Mdtr.Commands;

namespace ZFHandler.Helpers
{
    public class TransformerOption<T>
    {
        public  Func<T, CancellationToken?,ValueTask<IEnumerable<DownloadJob>>>? TransformFunction; 
    }
    public class IncomingWebhookTransformer<TRequest> : IPipelineBehavior<TRequest, IEnumerable<DownloadJob>> where TRequest : notnull
    {
        private readonly Func<TRequest, CancellationToken?,ValueTask<IEnumerable<DownloadJob>>> _transformer;

        public IncomingWebhookTransformer(IOptions<TransformerOption<TRequest>> optionsSnapshot)
        {
            var val = optionsSnapshot.Value ;
            _transformer = val?.TransformFunction ?? throw new ArgumentNullException(nameof(val));
        }
        public async Task<IEnumerable<DownloadJob>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<IEnumerable<DownloadJob>> next)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();
            var dlJob = await _transformer(request, cancellationToken);
            var s = await next();
            return dlJob;
        }
    }
    public class MediatRLoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull
    {
        private readonly ILogger<MediatRLoggingBehaviour<TRequest, TResponse>> _logger;
        public MediatRLoggingBehaviour(ILogger<MediatRLoggingBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            //Request
            _logger.LogInformation("Handling {Name}", typeof(TRequest).Name);
            var myType = request?.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType?.GetProperties() ?? Array.Empty<PropertyInfo>());
            foreach (PropertyInfo prop in props)
            {
                object? propValue = prop.GetValue(request, null);
                _logger.LogInformation("{Property} : {@Value}", prop.Name, propValue);
            }
            var response = await next();
            //Response
            _logger.LogInformation("Handled {Name}", typeof(TRequest).Name);
            return response;
        }
    }
}