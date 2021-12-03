using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace WebhookFileMover.Database.Models
{
    public class JobProvider : IJobProvider
    {
        private readonly DatabaseConfig _databaseConfig;

        public JobProvider(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }

        public async Task<IEnumerable<Job>> Get()
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            // return await connection.QueryAsync<Job>("SELECT rowid AS Id, Name, Source FROM Jobs;");
            return await connection.GetAllAsync<Job>();
        }

        public async Task<Job> GetSingle(int id)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            
            
            var job = await connection.GetAsync<Job>(id);

            job.JobTaskInstances = await GetTasksForJobAsync(id);
            return job;
        }

        public async Task<IEnumerable<JobTaskInstance>> GetTasksForJobAsync(Job job)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            return await connection.QueryAsync<JobTaskInstance>("SELECT * FROM JobTaskInstances where ParentJob = @Id", job.Id);
        }

        public async Task<IEnumerable<JobTaskInstance>> GetTasksForJobAsync(int jobId)
        {
            await using var connection = new SqliteConnection(_databaseConfig.Name);
            var db = new DynamicParameters();
            db.Add("@Id", jobId);
            return await connection.QueryAsync<JobTaskInstance>("SELECT * FROM JobTaskInstances where ParentJob = @Id", db);
        }
    }
}