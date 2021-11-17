using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using ZFHandler.Controller;
using ZFHandler.Models.ConfigurationSchemas;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Mdtr.Handlers;
using ZFHandler.Models;
using ZFHandler.Services;
using ZoomFileManager.Models;

namespace ZFHandler.CustomBuilders
{
    // public class WebhookConfig<T> where T : IValidator<T>, IDownloadService<T>
    // {
    //     public string Identifier { get; set; }
    //     public string Route { get; set; }
    //     public IValidator<T> Implementer { get; }
    // }

    public static class ReceiverBuilder
    {
        public static IServiceCollection AddReceivers(this IServiceCollection services,
            Assembly? _assembly = null)
        {
            ServiceRegistrar.AddRequiredServices(services, new MediatRServiceConfiguration());

            var assembly = _assembly ?? typeof(ReceiverBuilder).Assembly;
            var candidates = assembly.GetExportedTypes()
                .Where(x =>
                {
                    if (x?.GetCustomAttributes<CreateReceiverAttribute>().Any() ?? false)
                    {
                        return x?.GetInterfaces()
                            .Where(i => i.IsGenericType)
                            .Select(i => i.GetGenericTypeDefinition())
                            .Contains(typeof(IRConv<>)) ?? false;
                    }

                    return false;
                });
            var enumerable = candidates as Type[] ?? candidates.ToArray();
            IEnumerable<Type> types = new[] { typeof(Zoominput) }; 
              
            
            services.TryAddTransient<IRequestHandler<DownloadJob,FileInfo?>, DownloadJobHandler>();
            services.TryAddSingleton<INotificationHandler<DownloadJobBatch>, BrokerService>();
            
            
            services
                .AddTransient<INotificationHandler<UploadJobSpec<SharepointClientConfig>>,
                    UploadJobHandler<SharepointClientConfig>>();
            services
                .AddTransient<INotificationHandler<UploadJobSpec<OD_DriveClientConfig>>,
                    UploadJobHandler<OD_DriveClientConfig>>();
            services
                .AddTransient<INotificationHandler<UploadJobSpec<DropBoxClientConfig>>,
                    UploadJobHandler<DropBoxClientConfig>>();
            services
                .AddTransient<INotificationHandler<UploadJobSpec<OD_UserClientConfig>>,
                    UploadJobHandler<OD_UserClientConfig>>();

            
            
            services.AddControllers(options =>
                    options.Conventions.Add(new GenericControllerRouteConvention()))
                .ConfigureApplicationPartManager(
                    m => m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider(types)));


            // services.Configure<AppConfig>("test", x => x.AllowedTokens = new []{"test"});
            // services.TryAddEnumerable(config.Select(x=>ServiceDescriptor.Scoped<ControllerBase,WebReceiver<T, TY>>()));
            services.TryAddTransient<IRequestHandler<Zoominput, DownloadJobBatch>, ReceiverTransformHandler<Zoominput>>();

            return services;
        }

        // public static IApplicationBuilder UseHandler<T, TY>(this IApplicationBuilder app, Func<T, TY> f)
        //     where TY : IResponseHandler
        // {
        //     
        // }

        // public static void MapReceiver<T>(this IEndpointRouteBuilder endpoints,
        //     WebhookConfig<T> config) where T : Object, IWebhookReceiverHandler<T>, IValidator<T>
        // {
        //     if (endpoints == null)
        //         throw new ArgumentNullException(nameof(endpoints));
        //     endpoints.
        //     // endpoints.MapControllerRoute(config.Identifier, config.Route, );
        // }

        // private static IEndpointConventionBuilder MapReceiversCore<T>(IEndpointRouteBuilder endpoints, WebhookConfig<T> config ,string pattern) where T : IValidator<T>
        // {
        //     // if (endpoints.ServiceProvider.GetService(typeof()) == null)
        //     var pipeline = endpoints.CreateApplicationBuilder().UseMidd
        // }
        // public static IApplicationBuilder AddReceiver<T>(this IApplicationBuilder builder, WebhookConfig<T> config)
        //     where T : IValidator<T>
        // {
        //     builder.Map(config.Route, a => { });
        //     
        //     return builder;
        // }
    }


    public static class UploaderBuilder
    {
        public static IServiceCollection AddUploader(this IServiceCollection services, AppConfig config)
        {
            if (config.UploadConfigs != null)
                foreach (var configUploadConfig in config.UploadConfigs)
                {
                    services.Configure<UploadTargetConfig>(configUploadConfig.Identifier, targetConfig =>
                    {
                        targetConfig.Identifier = configUploadConfig.Identifier;
                        targetConfig.ClientConfig = configUploadConfig.ClientConfig;
                        targetConfig.RootPath = configUploadConfig.RootPath;
                    });
                }

            // services.Configure<UploadTargetConfig>();
            return services;
        }
    }
}