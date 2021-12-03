using System;
using System.Collections.Generic;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Models.Configurations.Internal
{
    public class ResolvedReceiverConfiguration
    {
        public string Id { get; set; }

        public IEnumerable<ResolvedUploadTarget> ResolvedUploadTargets { get; set; } =
            Array.Empty<ResolvedUploadTarget>();

        public IEnumerable<NotificationProviderConfig> NotificationProviderConfigs { get; set; } =
            Array.Empty<NotificationProviderConfig>();
    }
}