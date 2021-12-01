using System.Threading;
using System.Threading.Tasks;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Models.Interfaces;

namespace WebhookFileMover.Providers.Dropbox
{
    public class DropboxProvider : IBaseUploadProvider
    {
        public async Task UploadFileAsync(ResolvedUploadJob uploadJobSpec, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}