using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Models.Interfaces
{
    public interface IDownloadJobHandler
    {
        Task<CompletedDownloadJob> HandleDownloadJobAsync(DownloadJob downloadJob, CancellationToken cancellationToken = default);
    }
}