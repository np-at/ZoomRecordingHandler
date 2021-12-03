using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebhookFileMover.Database.Models
{
    public interface IJobProvider
    {
        Task<IEnumerable<Job>> Get();
        Task<Job> GetSingle(int id);
        Task<IEnumerable<JobTaskInstance>> GetTasksForJobAsync(Job job);
        Task<IEnumerable<JobTaskInstance>> GetTasksForJobAsync(int jobId);
    }
}