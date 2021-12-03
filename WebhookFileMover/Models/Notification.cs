using WebhookFileMover.Database.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Models
{
    public class Notification
    {
        public NotificationProviderConfig? NotificationProviderConfig { get; set; }
        public string? WebEventStringBody { get; set; }
        public Job? Job { get; init; }

        // public string GenerateFailureMessage()
        // {
        //     var resolvedParamBag = this.ResolveTemplateParams();
        //     return StringUtils.ApplyTemplatedFormattingString(
        //         NotificationProviderConfig.FailureMessageTemplate ?? throw new InvalidOperationException(), resolvedParamBag);
        // }
       
    }
    
}