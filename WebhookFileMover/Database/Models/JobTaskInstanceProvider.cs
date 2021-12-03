using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace WebhookFileMover.Database.Models
{
    public class JobTaskInstanceProvider : IJobTaskInstanceProvider
    {
        private readonly DatabaseConfig _databaseConfig;
        public JobTaskInstanceProvider(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }
        public async Task<IEnumerable<JobTaskInstance>> GetAllForParent(Job parentJob,CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            return await connection.QueryAsync<JobTaskInstance>("SELECT * FROM JobTaskInstances WHERE ParentJob = @Id", parentJob.Id);
        }

        public async Task<JobTaskInstance> GetSingle(int id, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            return await connection.GetAsync<JobTaskInstance>(id);
        }
        
    }
}