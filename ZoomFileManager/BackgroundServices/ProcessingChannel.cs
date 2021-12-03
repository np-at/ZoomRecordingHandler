// using System.Threading.Channels;
// using System.Threading.Tasks;
// using System;
// using System.Collections.Generic;
// using System.Threading;
// using Microsoft.Extensions.Logging;
// using ZFHandler.Models;
// using ZoomFileManager.Models;
//
//
// namespace ZoomFileManager.BackgroundServices
// {
//     public class PChannel<T> : Channel<T>
//     {
//    
//         private readonly Channel<T> _eventChannel;
//
//         private PChannel()
//         {
//       
//             var options = new UnboundedChannelOptions()
//             {
//                 
//                 SingleReader = false,
//                 SingleWriter = true,
//                 AllowSynchronousContinuations = false
//             };
//             _eventChannel = Channel.CreateUnbounded<T>(options);
//         }
//
//         public static PChannel<T> CreateInstance()
//         {
//             return new PChannel<T>();
//         }
//
//         public async Task<bool> AddEventAsync(T channelEvent, CancellationToken ct = default)
//         {
//             while (await _eventChannel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested)
//             {
//                 if (_eventChannel.Writer.TryWrite(channelEvent))
//                 {
//                     return true;
//                 }
//             }
//             return false;
//         }
//         public ValueTask<bool> WaitToReadChannelEventAsync(CancellationToken ct = default) =>
//             _eventChannel.Reader.WaitToReadAsync(ct);
//         public ValueTask<T> ReadChannelEventAsync(CancellationToken ct = default) => _eventChannel.Reader.ReadAsync(ct);
//         public bool TryCompleteWriter(Exception? ex = null) => _eventChannel.Writer.TryComplete(ex);
//
//         public IAsyncEnumerable<T> ReadAllEventsAsync(CancellationToken ct = default) =>
//             _eventChannel.Reader.ReadAllAsync(ct);
//         
//         
//
//     }
//
//     
//     public class ProcessingChannel
//     {
//         private readonly ILogger<ProcessingChannel> _logger;
//         private readonly PChannel<string> _channel;
//         private readonly PChannel<Zoominput> _zoomEventChannel;
//         private readonly PChannel<UploadJobSpec> _uploadJobChannel;
//         
//         public ProcessingChannel(ILogger<ProcessingChannel> logger)
//         {
//             _logger = logger;
//             _channel = PChannel<string>.CreateInstance();
//             var options = new UnboundedChannelOptions()
//             {
//                 SingleReader = true,
//                 SingleWriter = true,
//                 AllowSynchronousContinuations = false
//             };
//             // _zoomEventChannel = Channel.CreateUnbounded<Zoominput>(options);
//             _zoomEventChannel = PChannel<Zoominput>.CreateInstance();
//             _uploadJobChannel = PChannel<UploadJobSpec>.CreateInstance();
//         }
//
//         internal static class EventIds
//         {
//             public static readonly EventId ChannelMessageWritten = new EventId(100, "ChannelMessageWritten");
//         }
//
//         // public async Task<bool> AddZoomEventAsync(Zoominput Zoominput, CancellationToken ct = default)
//         // {
//         //     while (await _zoomEventChannel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested)
//         //     {
//         //         if (_zoomEventChannel.Writer.TryWrite(Zoominput))
//         //         {
//         //             return true;
//         //         }
//         //     }
//         //     return false;
//         // }
//
//         public async Task<bool> AddZoomEventAsync(Zoominput Zoominput, CancellationToken ct = default) =>
//             await _zoomEventChannel.AddEventAsync(Zoominput, ct);
//         // ZOOM EVENTS
//         public ValueTask<bool> WaitToReadZoomEventAsync(CancellationToken ct = default) =>
//             _zoomEventChannel.WaitToReadChannelEventAsync(ct);
//
//         public ValueTask<Zoominput> ReadZoomEventAsync(CancellationToken ct = default) =>
//             _zoomEventChannel.ReadChannelEventAsync(ct);
//             // _zoomEventChannel.Reader.ReadAsync(ct);
//
//             public IAsyncEnumerable<Zoominput> ReadAllZoomEventsAsync(CancellationToken ct = default) =>
//                 _zoomEventChannel.ReadAllEventsAsync(ct);
//             // _zoomEventChannel.Reader.ReadAllAsync(ct);
//
//         // UPLOAD JOBS
//         public ValueTask<bool> WaitToReadUploadJobAsync(CancellationToken ct = default) =>
//             _uploadJobChannel.WaitToReadChannelEventAsync(ct);
//
//         public ValueTask<UploadJobSpec> ReadUploadJobAsync(CancellationToken ct = default) =>
//             _uploadJobChannel.ReadChannelEventAsync(ct);
//
//         public IAsyncEnumerable<UploadJobSpec> ReadAllUploadJobAsync(CancellationToken ct = default) =>
//             _uploadJobChannel.ReadAllEventsAsync(ct);
//
//         
//         // 
//         public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
//         {
//             while (await _channel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested)
//             {
//                 if (_channel.Writer.TryWrite(fileName))
//                 {
//                     
//                     return true;
//                 }
//             }
//
//             return false;
//         }
//
//         public IAsyncEnumerable<string> ReadAllStringAsync(CancellationToken ct = default) =>
//             _channel.Reader.ReadAllAsync(ct);
//
//         public bool TryCompleteWriter(Exception? ex = null) => _channel.Writer.TryComplete(ex);
//
//         // private static class Log
//         // {
//         //     private static readonly Action<ILogger, string, Exception> _channelMessageWritten = LoggerMessage.Define()
//         //     {
//         //         
//         //     }
//         // }
//     }
// }