namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive
{
    public class SharepointClientConfig : BaseOneDriveClientConfig
    {
        public string? SiteName { get; set; }

        public SharepointClientConfig()
        {
        }

        public SharepointClientConfig(ClientConfig clientConfig)
        {
            SiteName = clientConfig.SiteRelativePath;
            ClientId = clientConfig.ClientId;
            ClientSecret = clientConfig.ClientSecret;
            RootDirectory = clientConfig.RootDirectory;
            TenantId = clientConfig.TenantId;
        }
    }
}