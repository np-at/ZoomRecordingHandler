using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebhookFileMover.Extensions;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;


namespace RecordingHandler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appConfig = Configuration.GetSection("AppConfig").Get<AppConfig>();
            services.AddHealthChecks();
            
            // services.AddControllers();
            // services.AddTransient<IWebhookDownloadJobTransformer<ZoomWebhook>, ZoomWebhookTransformer>();
            // services.TestAddR(new[] { typeof(ZoomWebhook).GetTypeInfo() },
            //     Configuration.GetSection("AppConfig").Get<AppConfig>());
            services.InitializeReceiverBuilder()
                .RegisterReceiver<ZoomWebhook>(appConfig)
                .RegisterDownloadHandler()
                .RegisterWebhookTransformer<ZoomWebhookTransformer>()
                .RegisterDefaultUploadProviders()
                .RegisterCustomEndpoint("testing")
                .Build();
            services.FinalizeWebhookFileMoverRegistrations();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();
            app.UseHealthChecks("/healthcheck");
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthcheck");
            });
        }
    }
}