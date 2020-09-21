using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using ZoomFileManager.Services;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Security.Claims;

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
            services.AddHttpClient();
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());

            services.AddSingleton(fileProvider);
            #region Authentication
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = SchemesNamesConst.TokenAuthenticationDefaultScheme;
            })
            .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(SchemesNamesConst.TokenAuthenticationDefaultScheme, o => { });
            #endregion
            services.AddScoped<OneDriveOperationsService>();
            services.AddScoped<RecordingManagementService>();
            //services.AddAuthentication("Anonymous").AddScheme("Anonymous", o=>o.;
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
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }


        public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
        {
            public IServiceProvider ServiceProvider { get; set; }

            public TokenAuthenticationHandler(IOptionsMonitor<TokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
                : base(options, logger, encoder, clock)
            {
                ServiceProvider = serviceProvider;
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var headers = Request.Headers;
                var token = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(token))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Token is null"));
                }

                bool isValidToken = false; // check token here

                if (!isValidToken)
                {
                    return Task.FromResult(AuthenticateResult.Fail($"Balancer not authorize token : for token={token}"));
                }

                var claims = new[] { new Claim("token", token) };
                var identity = new ClaimsIdentity(claims, nameof(TokenAuthenticationHandler));
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }
        public static class SchemesNamesConst
        {
            public const string TokenAuthenticationDefaultScheme = "TokenAuthenticationScheme";
        }

    }

    public class TokenAuthenticationOptions : AuthenticationSchemeOptions
    {

    }
}