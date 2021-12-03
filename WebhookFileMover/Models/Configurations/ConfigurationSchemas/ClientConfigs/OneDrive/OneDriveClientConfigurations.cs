namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive
{
    public class OD_DriveClientConfig : BaseOneDriveClientConfig
    {
        // public string? DriveId { get; set; }

        public OD_DriveClientConfig()
        {
        }

        public OD_DriveClientConfig(ClientConfig clientConfig)
        {
            DriveId = clientConfig.DriveId;
            ClientId = clientConfig.ClientId;
            ClientSecret = clientConfig.ClientSecret;
            RootDirectory = clientConfig.RootDirectory;
            TenantId = clientConfig.TenantId;
        }
    }
}