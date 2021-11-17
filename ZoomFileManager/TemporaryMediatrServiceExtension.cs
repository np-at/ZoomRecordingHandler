using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZFHandler.Controller;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Mdtr.Handlers;
using ZFHandler.Models;
using ZFHandler.Models.ConfigurationSchemas;
using ZFHandler.Services;
using ZoomFileManager.Models;
using ZoomFileManager.Services;
using JobTracker = ZFHandler.Services.JobTracker;

namespace ZoomFileManager
{
    public static class TemporaryMediatrServiceExtension
    {
        public static IServiceCollection AddTemporaryMediatrConfig(this IServiceCollection services)
        {
            ServiceRegistrar.AddRequiredServices(services, new MediatRServiceConfiguration());

            var assembly = typeof(Startup).Assembly;
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

        private static IServiceCollection AddType<T>(this IServiceCollection services, T type) where T : IRConv<T>, new()
        {
            services.TryAddTransient<IRequestHandler<T, DownloadJobBatch>, ReceiverTransformHandler<T>>();
            return services;
        }
    }
}