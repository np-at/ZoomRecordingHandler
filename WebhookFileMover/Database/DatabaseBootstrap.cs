using System;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace WebhookFileMover.Database
{
    public class DatabaseBootstrap : IDatabaseBootstrap
    {
        private readonly DatabaseConfig _databaseConfig;
        private readonly ILogger<DatabaseBootstrap> _logger;

        public DatabaseBootstrap(DatabaseConfig databaseConfig, ILogger<DatabaseBootstrap> logger)
        {
            this._databaseConfig = databaseConfig;
            _logger = logger;
        }

        public void Setup()
        {
            try
            {
                
                using var connection = new SqliteConnection(_databaseConfig.Name);
                try
                {
                    var fi = new FileInfo(connection.DataSource);
                    if  (!fi.Directory?.Exists ?? false)
                        fi.Directory?.Create();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

#if DEBUG
                connection.Execute(@"drop table if exists JobTaskInstances;
                                     drop table if exists Jobs;");
#endif
                var table = connection.Query<string>(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name = 'Jobs';");
                var tableName = table.FirstOrDefault();
                if (!string.IsNullOrEmpty(tableName) && tableName == "Jobs")
                    return;

                // connection.Execute("Create Table Jobs (Name VARCHAR(100) NOT NULL,Source VARCHAR(1000) NOT NULL);");
                connection.Execute(@"create table Jobs (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT
                                        constraint Jobs_pk,
                                        
                                        Name varchar(100),
                                        Source Varchar(50),
                                        RawMessage TEXT,
                                        AssociatedReceiverId varchar(16)
                                    );
                                     create table JobTaskInstances (
      	                                Id INTEGER primary key autoincrement
      		                                constraint JobTaskInstances_pk,
      	                                ParentJob INTEGER
      		                                constraint JobTaskInstances_Jobs_Id_fk
      			                                references Jobs,
      			                        JobType smallint not null,
                                        Status smallint NOT NULL,
                                        LocationUri varchar(100) 
                                      );
                                    
                                    create unique index Jobs_Id_uindex
                                        on Jobs (Id);
                                    
                                    create unique index JobTaskInstances_Id_uindex
                                        on JobTaskInstances (Id);"
                );
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error during database bootstrapping");
                throw;
            }
        }
    }
}