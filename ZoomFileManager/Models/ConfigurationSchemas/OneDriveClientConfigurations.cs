namespace ZoomFileManager.Models.ConfigurationSchemas
{
    public abstract class BaseOneDriveClientConfig
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? RootDirectory { get; set; }

    }

    public class OD_UserClientConfig : BaseOneDriveClientConfig
    {
        public string? UserName { get; set; }
    }
    public class OD_DriveClientConfig : BaseOneDriveClientConfig
    {
        public string? DriveId { get; set; }

    }

    public class SharepointClientConfig : BaseOneDriveClientConfig
    {
        public string? SiteName { get; set; }

    }

}