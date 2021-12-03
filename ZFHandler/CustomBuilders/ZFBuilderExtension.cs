using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace ZFHandler.CustomBuilders
{
    public static class ZFBuilderExtension
    {
        public static IServiceCollection AddZF(this IServiceCollection services, Action<AppConfig> appConfig)
        {
            
            return services;
        }

        public static IApplicationBuilder UseZF(this IApplicationBuilder app)
        {
            return app;
        }

        public static IEndpointConventionBuilder MapZF(this IEndpointRouteBuilder endpoints)
        {
            // return endpoints
            throw new System.NotImplementedException();
        }
        
    }
}