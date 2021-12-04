using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebhookFileMover.Database;
using WebhookFileMover.Extensions;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Providers.Notifications;

#if DEBUG
using WebhookFileMover.Middleware;
#endif

namespace RecordingHandler
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
            var appConfig = Configuration.GetSection("AppConfig").Get<AppConfig>();
            services.AddHealthChecks();
            services.AddWfmDatabaseConfiguration(Configuration.GetConnectionString("Default"));
  
            services.InitializeReceiverBuilder()
                .RegisterReceiverConfig<ZoomWebhook>(appConfig)
                .RegisterDownloadHandler()
                .RegisterWebhookTransformer<ZoomWebhookTransformer>()
                .RegisterDefaultUploadProviders()
                .RegisterEndpointFromConfig()
                .RegisterCustomTemplateResolutionFunction<SlackNotificationProvider>("GetUserIdAsync")
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
            app.UseWfmDatabaseBootstrap();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthcheck");
#if DEBUG
                endpoints.MapGraphVisualisation("/graph");
#endif
            });
        }
    }
}