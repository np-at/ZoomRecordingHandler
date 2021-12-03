namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs
{
    // Used for categorizing inherited generic restrictions
    public abstract class BaseClientConfig
    {
        
        public FileExistsBehavior FileAlreadyExistsBehavior { get; set; }
    }
}