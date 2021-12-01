namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.Dropbox
{
    public class DropBoxClientConfig : BaseClientConfig
    {
        public string? ApiKey { get; set; }
        public string? AppSecret { get; set; }
        public string? RefreshToken { get; set; }

        public string? AdminTeamMemberId { get; set; }

        
    }
}