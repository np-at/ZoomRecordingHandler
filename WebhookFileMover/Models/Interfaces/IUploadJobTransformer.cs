using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Models.Interfaces
{
    public interface IUploadJobTransformer
    {
        ValueTask<IEnumerable<object>> TransformJob(CompletedDownloadJob completedDownloadJob,
            CancellationToken cancellationToken = default);
    }
}