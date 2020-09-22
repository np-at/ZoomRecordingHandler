using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ZoomFileManager.Services;
using ZoomFileManager.Controllers;
using System;
using ZoomFileManager.Helpers;

namespace ZoomFileManager
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddHttpClient();
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());

            services.AddSingleton(fileProvider);
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = SchemesNamesConst.TokenAuthenticationDefaultScheme;
            })
            .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(SchemesNamesConst.TokenAuthenticationDefaultScheme, o => { });
            services.AddScoped<OneDriveOperationsService>();
            services.AddTransient<RecordingManagementService>();
            services.AddTransient<Odru>();
            services.Configure((Action<WebhookRecieverOptions>)(o =>
            {
                o.AllowedTokens =  Configuration.GetSection("AppConfig").GetSection("allowedTokens").Get<string[]>();;

            }));
 
            services.Configure<OdruOptions>(x => Configuration.GetSection("AppConfig").Bind("OdruOptions", x));
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHealthChecks(new PathString("healthcheck"));
            //app.UseHttpsRedirection();
           
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}