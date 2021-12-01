using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Models.Interfaces
{
    public interface IWebhookDownloadJobTransformer<in T>
    {
        ValueTask<IEnumerable<DownloadJob>> TransformWebhook(T webhook, CancellationToken cancellationToken = default);
    }
}