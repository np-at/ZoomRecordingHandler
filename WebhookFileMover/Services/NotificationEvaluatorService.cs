using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebhookFileMover.Channels;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Services
{
    public interface INotificationEvaluator
    {
        Task HandleFinishedTaskAsync(int taskInstanceId);
    }

    public class NotificationEvaluatorService : INotificationEvaluator
    {
        private readonly IJobProvider _jobProvider;
        private readonly IJobTaskInstanceProvider _jobTaskInstanceProvider;
        private readonly ILogger<NotificationEvaluatorService> _logger;
        private readonly IOptionsMonitor<ResolvedReceiverConfiguration> _receiverOptionsSnapshot;
        private readonly JobQueueChannel _jobQueueChannel;

        public NotificationEvaluatorService(ILogger<NotificationEvaluatorService> logger, IJobTaskInstanceProvider jobTaskInstanceProvider,
            IJobProvider jobProvider, IOptionsMonitor<ResolvedReceiverConfiguration> receiverOptionsSnapshot, JobQueueChannel jobQueueChannel)
        {
            _logger = logger;
            _jobTaskInstanceProvider = jobTaskInstanceProvider;
            _jobProvider = jobProvider;
            _receiverOptionsSnapshot = receiverOptionsSnapshot;
            _jobQueueChannel = jobQueueChannel;
        }

        private async Task FireNotifications(Job job)
        {
            // string messageTemplate =   $"{(string.IsNullOrWhiteSpace(webhookEvent.Payload.Object.HostEmail) ? string.Empty : "<@" + userId + '>')}Successfully uploaded recording: {webhookEvent.Payload.Object.Topic}. You can view them using this url: <{_serviceOptions.ReferralUrlBase + itemResponseWebUrl.Remove(itemResponseWebUrl.LastIndexOf('/'))}| onedrive folder link>";
            // var notification = new Notification()
            // {
            //     Job = job,
            //     NotificationConfigId = 
            // }
            var resolvedReceiverConfiguration = _receiverOptionsSnapshot.Get(job.AssociatedReceiverId) ?? throw new KeyNotFoundException("Unable to locate corresponding Receiver id for job");
            foreach (var notificationProviderConfig in resolvedReceiverConfiguration.NotificationProviderConfigs)
            {
                var notification = new Notification
                {
                    Job = job,
                    NotificationProviderConfig = notificationProviderConfig,
                    WebEventStringBody = job.RawMessage ?? string.Empty
                };
                await _jobQueueChannel.AddNotificationAsync(notification).ConfigureAwait(false);
            }
            
            
#if DEBUG
            _logger.LogInformation("Notification fired: {@Job}", job);
            await Task.Delay(1000);
#endif

            
            
        }

        public async Task HandleFinishedTaskAsync(int taskInstanceId)
        {
            var task = await _jobTaskInstanceProvider.GetSingle(taskInstanceId);
            var parentJob = await AreChildJobsFinishedAsync(task.ParentJob);
            if (parentJob == null)
                return;
            await FireNotifications(parentJob);
        }

        private async Task<Job?> AreChildJobsFinishedAsync(int parentJobId)
        {
            var parent = await _jobProvider.GetSingle(parentJobId);

            if (parent.JobTaskInstances?.All(x =>
                    x.Status is TaskInstanceStatus.Finished or TaskInstanceStatus.Failed) ??
                throw new Exception($"No children Task instances found for parent job: {parent}"))
                return parent;
            return null;
        }
    }
}