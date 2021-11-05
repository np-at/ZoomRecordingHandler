using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using ZoomFileManager.BackgroundServices;
using ZoomFileManager.Controllers;
using ZoomFileManager.Helpers;
using ZoomFileManager.Models;
using ZoomFileManager.Models.ConfigurationSchemas;
using ZoomFileManager.Services;

namespace ZoomFileManager
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            var appConfigOptions = Configuration.GetSection("AppConfig").Get<AppConfig>();
            services.AddHealthChecks();
            services.AddHttpClient();
            services.AddHttpClient("dropbox", c =>
            {
                c.Timeout = TimeSpan.FromMinutes(10);
            });
            services.Configure<RecordingManagementServiceOptions>(x =>
            {
                x.Endpoints = appConfigOptions.NotificationOptions?.Endpoints;
            });
            services.Configure((Action<WebhookReceiversOptions>) (o =>
            {
                o.AllowedTokens = appConfigOptions.AllowedTokens;
            }));
            // services.Configure<SlackApiOptions>(x => appConfigOptions.Bind("SlackApiOptions", x));
            // services.Configure<OneDriveClientConfig>(x => appConfigOptions.Bind("OneDriveClientConfig", x));
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());
            // services.AddSingleton<PChannel<ZoomWebhookEvent>>();
            services.AddSingleton<ProcessingChannel>();
            services.AddHostedService<ZoomEventProcessingService>();
            services.AddSingleton(fileProvider);
            services.AddAuthentication(o => { o.DefaultScheme = SchemesNamesConst.TokenAuthenticationDefaultScheme; })
                .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(
                    SchemesNamesConst.TokenAuthenticationDefaultScheme, _ => { });
            services.AddTransient<OneDriveOperationsService>();
            services.AddTransient<RecordingManagementService>();
            services.AddTransient<IDropboxOperations, DropboxOperationsService>();
            services.AddTransient<SlackApiHelpers>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSerilogRequestLogging();
            }

            app.UseHealthChecks("/healthcheck");
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthcheck");
            });
        }
    }
}