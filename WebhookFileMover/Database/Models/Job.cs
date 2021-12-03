using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace WebhookFileMover.Database.Models
{
    [Table("Jobs")]
    public class Job
    {
        [Key] public int Id { get; set; }
        public string? Name { get; set; }
        public string? Source { get; set; }
        public string? RawMessage { get; set; }
        public string? AssociatedReceiverId { get; set; }
        [Computed] public IEnumerable<JobTaskInstance>? JobTaskInstances { get; set; }
    }
}