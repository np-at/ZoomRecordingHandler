using System;
using System.IO;
using System.Reflection;
using MediatR;
using MediatR.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using WebhookFileMover.Extensions;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;
using ZFHandler.CustomBuilders;
using ZFHandler.Services;
using ZoomFileManager.Controllers;
using ZoomFileManager.Helpers;
using ZoomFileManager.Models;

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
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());

            var appConfigOptions = Configuration.GetSection("AppConfig").Get<AppConfig>();
            // services.Configure<BrokerServiceOptions>(o =>
            // {
            //     o.UploadTargetConfigs = appConfigOptions.UploadConfigs;
            //     o.UploadTargets = appConfigOptions.UploadTargets;
            // });
         

         
            
            
            
            services.AddHealthChecks();
            services.AddHttpClient();
            services.AddHttpClient("dropbox", c => { c.Timeout = TimeSpan.FromMinutes(10); });
            // services.Configure<RecordingManagementServiceOptions>(x =>
            // {
            //     x.Endpoints = appConfigOptions.NotificationOptions?.Endpoints;
            // });
            services.Configure((Action<WebhookReceiversOptions>)(o =>
            {
                o.AllowedTokens = appConfigOptions.AllowedTokens;
            }));
            // services.Configure<SlackApiOptions>(x => appConfigOptions.Bind("SlackApiOptions", x));
            // services.Configure<OneDriveClientConfig>(x => appConfigOptions.Bind("OneDriveClientConfig", x));
            // services.AddSingleton<ProcessingChannel>();
            // services.AddHostedService<ZoomEventProcessingService>();
            services.AddSingleton<IFileProvider>(fileProvider);
            services.AddAuthentication(o => { o.DefaultScheme = SchemesNamesConst.TokenAuthenticationDefaultScheme; })
                .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(
                    SchemesNamesConst.TokenAuthenticationDefaultScheme, _ => { });
            // services.AddTransient<OneDriveOperationsService>();
            // services.AddTransient<RecordingManagementService>();
            // services.AddTransient<IDropboxOperations, DropboxOperationsService>();
            // services.AddTransient<IDownloadService<Zoominput>, ZoomDownloadService>();
            //
            // services.AddTransient<SlackApiHelpers>();
            //
            // services.AddTransient(typeof(ZoomWebhookHandler));
            // services.AddControllers();
            // services.AddReceivers(config =>
            // {
            //     config.AllowedTokens = appConfigOptions.AllowedTokens;
            //     config.NotificationOptions = appConfigOptions.NotificationOptions;
            //     config.ReceiverConfigs = appConfigOptions.ReceiverConfigs;
            //     config.UploadConfigs = appConfigOptions.UploadConfigs;
            //     config.UploadTargets = appConfigOptions.UploadTargets;
            //     config.SlackApiOptions = appConfigOptions.SlackApiOptions;
            // });
            services.AddTransient<IWebhookDownloadJobTransformer<Zoominput>, ZoominputTransformer>();
            services.TestAddR(new [] { typeof(Zoominput).GetTypeInfo() }, appConfigOptions);
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
            // app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthcheck");
            });
        }
    }
}