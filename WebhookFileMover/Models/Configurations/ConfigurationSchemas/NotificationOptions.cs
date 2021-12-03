namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas
{
    public class NotificationOptions
    {
        public string[]? Endpoints { get; set; }
        public string? ReferralUrlBase { get; set; }
        public string[]? AllowedHostEmails { get; set; }
    }

    public class NotificationProvider
    {
        public string Endpoint { get; set;}
    }
}