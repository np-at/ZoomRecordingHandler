using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace WebhookFileMover.Database.Models
{
    class JobTaskInstanceRepository : IJobTaskInstanceRepository
    {
        private readonly DatabaseConfig _databaseConfig;

        public JobTaskInstanceRepository(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }

        public async Task Create(JobTaskInstance jobTask, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);

            var id = await connection.InsertAsync(jobTask);
            jobTask.Id = id;
        }

        public async Task Update(JobTaskInstance jobTask, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            var success = await connection.UpdateAsync<JobTaskInstance>(jobTask);
            if (!success)
                throw new DataException("error updating value");
        }

        public async Task UpdateStatusForTaskAsync(int jobTaskInstanceId, TaskInstanceStatus newStatus)
        {
            var dp = new DynamicParameters();
            dp.Add("@Status", newStatus,  DbType.UInt16);
            dp.Add("@Id", jobTaskInstanceId, DbType.UInt32);
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            await connection.ExecuteAsync("Update JobTaskInstances SET Status = @Status WHERE Id = @Id", dp);
        }
    }
}