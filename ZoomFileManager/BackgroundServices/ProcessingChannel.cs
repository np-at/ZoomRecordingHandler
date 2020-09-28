using System.Threading.Channels;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using ZoomFileManager.Models;
using ILogger = Serilog.ILogger;

namespace ZoomFileManager.BackgroundServices
{
    public class ProcessingChannel
    {
        private readonly ILogger<ProcessingChannel> _logger;
        private readonly Channel<string> _channel;
        private readonly Channel<ZoomWebhookEvent> _eventChannel;
        
        public ProcessingChannel(ILogger<ProcessingChannel> logger)
        {
            _logger = logger;
            var options = new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            };
            _eventChannel = Channel.CreateUnbounded<ZoomWebhookEvent>(options);
            _channel = Channel.CreateUnbounded<string>(options);
        }

        internal static class EventIds
        {
            public static readonly EventId ChannelMessageWritten = new EventId(100, "ChannelMessageWritten");
        }

        public async Task<bool> AddZoomEventAsync(ZoomWebhookEvent zoomWebhookEvent, CancellationToken ct = default)
        {
            while (await _eventChannel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
            {
                if (_eventChannel.Writer.TryWrite(zoomWebhookEvent))
                {
                    return true;
                }
            }

            return false;
        }

        public ValueTask<bool> WaitToReadZoomEventAsync(CancellationToken ct = default) =>
            _eventChannel.Reader.WaitToReadAsync(ct);
        public ValueTask<ZoomWebhookEvent> ReadZoomEventAsync(CancellationToken ct = default) =>
            _eventChannel.Reader.ReadAsync(ct);

        public IAsyncEnumerable<ZoomWebhookEvent> ReadAllZoomEventsAsync(CancellationToken ct = default) =>
            _eventChannel.Reader.ReadAllAsync(ct);
        public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
        {
            while (await _channel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
            {
                if (_channel.Writer.TryWrite(fileName))
                {
                    
                    return true;
                }
            }

            return false;
        }

        public IAsyncEnumerable<string> ReadAllStringAsync(CancellationToken ct = default) =>
            _channel.Reader.ReadAllAsync(ct);

        public bool TryCompleteWriter(Exception? ex = null) => _channel.Writer.TryComplete(ex);

        // private static class Log
        // {
        //     private static readonly Action<ILogger, string, Exception> _channelMessageWritten = LoggerMessage.Define()
        //     {
        //         
        //     }
        // }
    }
}