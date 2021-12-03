using System;
using System.Threading;
using System.Threading.Tasks;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Models.Interfaces
{
    public interface IBaseUploadProvider
    {
        Task<Uri?> UploadFileAsync(ResolvedUploadJob uploadJobSpec, CancellationToken cancellationToken = default);
    }
}