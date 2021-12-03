using Dapper.Contrib.Extensions;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Database.Models
{
    [Table("JobTaskInstances")]
    public class JobTaskInstance
    {
        [Key]
        public int Id { get; set; }
        public int ParentJob { get; set; }
        public JobTaskType JobType { get; set; }
        public TaskInstanceStatus Status { get; set; }
        
        public string? LocationUri { get; set; }
        
    }
}