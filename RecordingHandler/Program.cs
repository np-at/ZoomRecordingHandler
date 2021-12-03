using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace RecordingHandler
{
    public static class Program
    {
        static Program()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddJsonFile(Environment.GetEnvironmentVariable("ZF_SECRETS_FILE") ?? "/run/secrets/zoomFileManager_settings", true)
                .Build();
        }

        public static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Console.WriteLine("Development mode active");
            Log.Warning("Starting up");
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        internal static IConfiguration Configuration { get; }
        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls("http://0.0.0.0:8080");
                    webBuilder.UseSerilog();
                    webBuilder.UseConfiguration(Configuration);
                });
   
    }
}