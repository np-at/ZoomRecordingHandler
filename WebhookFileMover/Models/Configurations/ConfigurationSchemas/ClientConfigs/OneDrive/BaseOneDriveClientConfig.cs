using Azure.Identity;
using Microsoft.Graph;

namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs.OneDrive
{
    public abstract class BaseOneDriveClientConfig : ClientConfig
    {
        // public string? ClientId { get; set; }
        // public string? ClientSecret { get; set; }
        // public string? TenantId { get; set; }
        // public string? RootDirectory { get; set; }

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
    public static class OneDriveClientConfigExtensions
    {
        public static GraphServiceClient CreateNewGraphServiceClientInstance(this BaseOneDriveClientConfig clientConfig) => new(OnedriveHelpers.DoAuth(clientConfig));

        internal static class OnedriveHelpers
        {
            public static ClientSecretCredential DoAuth(BaseOneDriveClientConfig options) =>
                new(options.TenantId, options.ClientId, options.ClientSecret);
            
            public static ClientSecretCredential DoAuth(ClientConfig options) =>
                new(options.TenantId, options.ClientId, options.ClientSecret);
        }
    }
}