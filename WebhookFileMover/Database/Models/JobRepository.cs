using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper.Contrib.Extensions;

namespace WebhookFileMover.Database.Models
{
    public class JobRepository : IJobRepository
    {
        private readonly DatabaseConfig _databaseConfig;

        public JobRepository(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }

        public async Task Create(Job job)
        {
         await using var connection = new SqliteConnection(_databaseConfig.Name);
         var jobId = await connection.InsertAsync(job);
         job.Id = jobId;
         // await connection.ExecuteAsync(
         //     "INSERT INTO Jobs (Name, Source, raw_message)VALUES (@Name, @Description, @Raw_Message);", job);
        }
    }
}