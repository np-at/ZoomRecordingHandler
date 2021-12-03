using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Database.Models;

namespace WebhookFileMover.Database
{
    public static class WfmDatabaseExtensions
    {
        public static void AddWfmDatabaseConfiguration(this IServiceCollection services, string sqliteConnection)
        {
            try
            {
                services.AddSingleton(new DatabaseConfig { Name = sqliteConnection });
                services.AddTransient<IDatabaseBootstrap, DatabaseBootstrap>();
                services.AddTransient<IJobProvider, JobProvider>();
                services.AddTransient<IJobRepository, JobRepository>();
                services.AddTransient<IJobTaskInstanceProvider, JobTaskInstanceProvider>();
                services.AddTransient<IJobTaskInstanceRepository, JobTaskInstanceRepository>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void UseWfmDatabaseBootstrap(this IApplicationBuilder app)
        {
            try
            {
                var databaseBootstrap = app.ApplicationServices.GetRequiredService<IDatabaseBootstrap>();
                databaseBootstrap.Setup();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}