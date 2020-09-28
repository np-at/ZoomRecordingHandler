using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ZoomFileManager.BackgroundServices;
using ZoomFileManager.Controllers;
using ZoomFileManager.Helpers;
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
            var appConfigOptions = Configuration.GetSection("AppConfig");
            services.AddHttpClient();
            services.Configure<RecordingManagementServiceOptions>(x =>
            {
                try
                {
                    appConfigOptions.Bind("NotificationOptions", x);
                }
                catch (Exception)
                {
                    //ignore
                }
            });
            services.Configure((Action<WebhookReceiversOptions>) (o =>
            {
                o.AllowedTokens = Configuration.GetSection("AppConfig").GetSection("allowedTokens").Get<string[]>();
            }));

            services.Configure<OdruOptions>(x => appConfigOptions.Bind("OdruOptions", x));
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());
            services.AddSingleton<ProcessingChannel>();
            services.AddHostedService<ZoomEventProcessingService>();
            services.AddSingleton(fileProvider);
            services.AddAuthentication(o => { o.DefaultScheme = SchemesNamesConst.TokenAuthenticationDefaultScheme; })
                .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(
                    SchemesNamesConst.TokenAuthenticationDefaultScheme, o => { });
            services.AddTransient<OneDriveOperationsService>();
            services.AddTransient<RecordingManagementService>();
            services.AddTransient<OneDriveOperationsService>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            //app.UseHealthChecks(new PathString("healthcheck"));
            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            // app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}