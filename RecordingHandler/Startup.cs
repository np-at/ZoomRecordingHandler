using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebhookFileMover.Database;
using WebhookFileMover.Extensions;
using WebhookFileMover.Middleware;
using WebhookFileMover.Models;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;


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
            services.AddWFMDatabaseConfiguration(Configuration.GetConnectionString("Default"));
            // services.AddControllers();
            // services.AddTransient<IWebhookDownloadJobTransformer<ZoomWebhook>, ZoomWebhookTransformer>();
            // services.TestAddR(new[] { typeof(ZoomWebhook).GetTypeInfo() },
            //     Configuration.GetSection("AppConfig").Get<AppConfig>());
            services.InitializeReceiverBuilder()
                .RegisterReceiverConfig<ZoomWebhook>(appConfig)
                .RegisterDownloadHandler()
                .RegisterWebhookTransformer<ZoomWebhookTransformer>()
                .RegisterDefaultUploadProviders()
                .RegisterEndpointFromConfig()
                .Build();
            // services.InitializeReceiverBuilder()
            //     .RegisterReceiverConfig<ZoomWebhook>(appConfig)
            //     .RegisterDownloadHandler()
            //     .RegisterWebhookTransformer<ZoomWebhookTransformer>()
            //     .RegisterDefaultUploadProviders()
            //     .Build();
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
            app.UseWFMDatabaseBootstrap();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthcheck");
                endpoints.MapGraphVisualisation("/graph");
            });
        }
    }
}