using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Database.Models
{
    public interface IJobTaskInstanceProvider
    {
        Task<JobTaskInstance> GetSingle(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<JobTaskInstance>> GetAllForParent(Job parentJob, CancellationToken cancellationToken = default);
    }
}