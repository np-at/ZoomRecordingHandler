using System.Threading.Tasks;

namespace WebhookFileMover.Database.Models
{
    public interface IJobRepository
    {
        Task Create(Job job);
    }
}