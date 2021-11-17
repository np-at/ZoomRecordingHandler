namespace ZFHandler.Models.ConfigurationSchemas
{
    public abstract class BaseClientConfig
    {
        
    }
    public abstract class BaseOneDriveClientConfig : BaseClientConfig
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? RootDirectory { get; set; }

        public BaseOneDriveClientConfig()
        {
            
        }

        public BaseOneDriveClientConfig(ClientConfig clientConfig)
        {
            ClientId = clientConfig.ClientId;
            ClientSecret = clientConfig.ClientSecret;
            RootDirectory = clientConfig.RootDirectory;
            TenantId = clientConfig.TenantId;
        }
    }

    public class OD_UserClientConfig : BaseOneDriveClientConfig
    {
        public string? UserName { get; set; }

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
    public class OD_DriveClientConfig : BaseOneDriveClientConfig
    {
        public string? DriveId { get; set; }

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

    public class SharepointClientConfig : BaseOneDriveClientConfig
    {
        public string? SiteName { get; set; }

        public SharepointClientConfig()
        {
            
        }

        public SharepointClientConfig(ClientConfig clientConfig)
        {
            SiteName = clientConfig.SiteName;
            ClientId = clientConfig.ClientId;
            ClientSecret = clientConfig.ClientSecret;
            RootDirectory = clientConfig.RootDirectory;
            TenantId = clientConfig.TenantId;
        }
    }
    

}