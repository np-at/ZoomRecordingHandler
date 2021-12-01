namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive
{
    public class OD_UserClientConfig : BaseOneDriveClientConfig
    {
        // public string? UserName { get; set; }

        public OD_UserClientConfig()
        {
        }

        public OD_UserClientConfig(ClientConfig clientConfig)
        {
            UserName = clientConfig.UserName;
            ClientId = clientConfig.ClientId;
            ClientSecret = clientConfig.ClientSecret;
            RootDirectory = clientConfig.RootDirectory;
            TenantId = clientConfig.TenantId;
        }
    }
}