using System.Threading;
using System.Threading.Tasks;

namespace WebhookFileMover.Database.Models
{
    public interface IJobTaskInstanceRepository
    {
        Task Create(JobTaskInstance jobTask, CancellationToken cancellationToken = default);
        Task Update(JobTaskInstance jobTask, CancellationToken cancellationToken = default);
        Task UpdateStatusForTaskAsync(int jobTaskInstanceId, TaskInstanceStatus newStatus);
    }
}