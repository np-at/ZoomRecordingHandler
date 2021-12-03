using System.IO;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas.ClientConfigs;

namespace WebhookFileMover.Providers.Download
{
    public class DownloadJobHandlerOptions
    {
        public string RootDirectory { get; set; } = Path.GetTempPath();

        public FileExistsBehavior FileExistsBehavior { get; set; } = FileExistsBehavior.Overwrite;
    }
}