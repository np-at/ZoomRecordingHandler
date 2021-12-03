using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs;

namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas
{
    public class UploadTarget
    {
        public string? Name { get; set; }
        public string? ConfigId { get; set; }
        
        /// <summary>
        /// Relative to the upload provider's configured root, where to place the parent directory
        /// </summary>
        public string? RelativeRootUploadPath { get; set; }

        public string? NamingTemplate { get; set; } = "%N.%E";

        public string? DirectoryNamingTemplate { get; set; } = "%D_%N" ; // "%Y%Y%Y%Y-%M%M-%D%D-%T-%S";

        public FileExistsBehavior FileExistsBehavior { get; set; } = FileExistsBehavior.Overwrite;
    }
}