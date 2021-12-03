using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Channels
{
    internal class PChannel<T> : Channel<T>
    {
        private readonly Channel<T> _eventChannel;

        private PChannel()
        {
            var options = new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _eventChannel = Channel.CreateBounded<T>(options);
        }

        public static PChannel<T> CreateInstance()
        {
            return new PChannel<T>();
        }

        public async Task<bool> AddEventAsync(T channelEvent, CancellationToken ct = default)
        {
            while (await _eventChannel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested)
            {
                if (_eventChannel.Writer.TryWrite(channelEvent))
                {
                    return true;
                }
            }

            return false;
        }

        public ValueTask<bool> WaitToReadChannelEventAsync(CancellationToken ct = default) =>
            _eventChannel.Reader.WaitToReadAsync(ct);

        public ValueTask<T> ReadChannelEventAsync(CancellationToken ct = default) => _eventChannel.Reader.ReadAsync(ct);
        public bool TryCompleteWriter(Exception? ex = null) => _eventChannel.Writer.TryComplete(ex);

        public IAsyncEnumerable<T> ReadAllEventsAsync(CancellationToken ct = default) =>
            _eventChannel.Reader.ReadAllAsync(ct);
    }

    public class JobQueueChannel
    {
        private readonly PChannel<DownloadJobBatch> _downloadQueue;
        private readonly PChannel<ResolvedUploadJob> _uploadQueue;
        private readonly PChannel<Notification> _notificationQueue;

        private readonly ILogger<JobQueueChannel> _logger;


        public JobQueueChannel(ILogger<JobQueueChannel> logger)
        {
            _logger = logger;
            _notificationQueue = PChannel<Notification>.CreateInstance();
            _downloadQueue = PChannel<DownloadJobBatch>.CreateInstance();
            _uploadQueue = PChannel<ResolvedUploadJob>.CreateInstance();
        }

        public Task<bool> AddNotificationAsync(Notification notification,
            CancellationToken cancellationToken = default) =>
            _notificationQueue.AddEventAsync(notification, cancellationToken);

        public ValueTask<Notification> ReadNotificationAsync(CancellationToken cancellationToken = default) =>
            _notificationQueue.ReadChannelEventAsync(cancellationToken);
        
        public Task<bool> AddDownloadJobBatchAsync(DownloadJobBatch downloadJobBatch,
            CancellationToken cancellationToken = default) =>
            _downloadQueue.AddEventAsync(downloadJobBatch, cancellationToken);

        public ValueTask<DownloadJobBatch> ReadDownloadJobBatchAsync(CancellationToken cancellationToken = default) =>
            _downloadQueue.ReadChannelEventAsync(cancellationToken);

        public Task<bool> AddUploadJobAsync(ResolvedUploadJob resolvedUploadTarget,
            CancellationToken cancellationToken = default) =>
            _uploadQueue.AddEventAsync(resolvedUploadTarget, cancellationToken);

        public ValueTask<ResolvedUploadJob> ReadUploadJobAsync(CancellationToken cancellationToken = default) =>
            _uploadQueue.ReadChannelEventAsync(cancellationToken);
    }
}