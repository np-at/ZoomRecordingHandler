using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZoomFileManager.Middleware
{
    public class ReceiverValidationMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<ReceiverValidationMiddleware> _logger;
        private readonly IOptionsMonitor<ReceiverValidationOptions> _optionsMonitor;
        private ReceiverValidationOptions _options;
        
        

        public ReceiverValidationMiddleware(RequestDelegate next, ILogger<ReceiverValidationMiddleware>  logger, IOptionsMonitor<ReceiverValidationOptions> optionsMonitor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
            _optionsMonitor = optionsMonitor;
            _options = _optionsMonitor.CurrentValue;
            _optionsMonitor.OnChange(options =>
            {
                _options = options;
                
            });

        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Authentication", out var authHeader))
            {
                bool isMatched = false;
                for (int i = authHeader.Count - 1; i >= 0; i--)
                {
                    string? currentToken = authHeader[i];
                    for (int o = _options.AllowedTokens.Length - 1; o >= 0; o--)
                    {
                        if (!currentToken.Equals(_options.AllowedTokens[o])) continue;
                        isMatched = true;
                        break;
                    }
                    
                }

                if (!isMatched)
                    context.ForbidAsync();
            }
            return _next(context);
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private IList<StringSegment> EnsureConfigured()
        // {
        //     
        // }
    }

    public class ReceiverValidationOptions
    {
        public string[] AllowedTokens { get; set; } = null!;
    }
}