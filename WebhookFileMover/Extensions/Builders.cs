using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using WebhookFileMover.BackgroundServices;
using WebhookFileMover.Channels;
using WebhookFileMover.Controllers;
using WebhookFileMover.Models.Configurations;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Interfaces;
using WebhookFileMover.Pipelines.TPL;
using WebhookFileMover.Providers.Download;
using WebhookFileMover.Providers.Dropbox;
using WebhookFileMover.Providers.OneDrive;

namespace WebhookFileMover.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static TypeInfo[] Types { get; set; } = Array.Empty<TypeInfo>();
        public static ConcurrentDictionary<Type, string> EndpointMapping = new ();
        /// <summary>
        /// Adds singleton and hosted services for WebhookFileMover application features.
        /// Should be called after all receivers have been registered.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IServiceCollection FinalizeWebhookFileMoverRegistrations(this IServiceCollection serviceCollection)
        {
   
            
            // Make sure HttpClient has been added (should be idempotent)
            serviceCollection.AddHttpClient();
            
            // Add File provider
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());
            serviceCollection.AddSingleton<IFileProvider>(fileProvider);



            if (!Types.Any())
                throw new Exception(
                    "No Types Registered for WebhookFile Mover.  Did you remember to call RegisterBrokerServices after Adding receiver types?");
            serviceCollection.TryAddSingleton<JobQueueChannel>();
            serviceCollection.AddHostedService<DownloadBrokerService>();
            serviceCollection.AddHostedService<UploadBrokerService>();
            // Configure webhook receivers for specified types
            serviceCollection.AddControllers().ConfigureApplicationPartManager(p => p.FeatureProviders.Add(
                new GenericControllerFeatureProvider(ServiceRegistrationExtensions.Types)));
            return serviceCollection;
        }

        public static WFMBuilder InitializeReceiverBuilder(this IServiceCollection serviceCollection) => new WFMBuilder(ref serviceCollection);
    }
    public class WFMBuilder
    {
        internal IServiceCollection ServiceCollection;
        internal AppConfig? AppConfig;
        public WFMBuilder(ref IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public WFMBuilder<T> RegisterReceiver<T>(AppConfig appConfig, bool debug = false)
        {
            return new WFMBuilder<T>(ref ServiceCollection)
            {
                AppConfig = appConfig
            };
        }
    }

    public class WFMBuilder<T> : WFMBuilder
    {
        public WFMBuilder(ref IServiceCollection serviceCollection) : base(ref serviceCollection)
        {
        }

        public WFMBuilder<T> RegisterCustomEndpoint(string endpoint)
        {
            if (!ServiceRegistrationExtensions.EndpointMapping.TryAdd(typeof(T), endpoint))
                throw new Exception("Error while attempting to add type to Custom Endpoint Mapping");
            return this;
        }
        public WFMBuilder<T> RegisterWebhookTransformer<TY>()  where TY : class, IWebhookDownloadJobTransformer<T>
        {
            ServiceCollection.TryAddTransient<IWebhookDownloadJobTransformer<T>, TY>();
            return this;
        }
        public WFMBuilder<T> RegisterDownloadHandler(Action<DownloadHandlerOptions>? action = null,
            IDownloadJobHandler? downloadJobHandler = null)
        {
       
            
            // Setup Downloading Providers and Configurations
            // TODO: Add ability to specify alternate download handler
            var downloadOptions = new DownloadHandlerOptions()
            {
                DownloadJobHandlerType = typeof(DefaultDownloadHandler)
            };
            
              action?.Invoke(downloadOptions);
              
              ServiceCollection.Configure<DownloadHandlerOptions>(o =>
              {
                 
                  o.DownloadJobHandlerType = downloadOptions.DownloadJobHandlerType;
              });
            
            
            var downloadHandlerType = (downloadJobHandler == null)
                ? typeof(DefaultDownloadHandler)
                : downloadJobHandler.GetType();
            ServiceCollection.AddTransient(downloadHandlerType);
            return this;
        }

        public WFMBuilder<T> RegisterDefaultUploadProviders()
        {
            // Setup upload providers
            ServiceCollection.AddTransient<OnedriveUserProvider>();
            ServiceCollection.AddTransient<OnedriveDriveProvider>();
            ServiceCollection.AddTransient<DropboxProvider>();
            ServiceCollection.AddTransient<SharepointProvider>();
            return this;
        }

        public void Build()
        {
            // Resolve and add target configs
            var targetIds = ServiceCollection.AddWFConfiguration(AppConfig).Select(x => x.Id);
            ServiceCollection.Configure<GenericReceiverControllerOptions>(options => { options.ConfigIds = targetIds; });
            
            ServiceRegistrationExtensions.Types =
                ServiceRegistrationExtensions.Types.Append(typeof(T).GetTypeInfo()).ToArray();

        }
    }

   
    public static class Builders
    {
        // public static IServiceCollection TestAddR(this IServiceCollection services, AppConfig appConfig) =>
        //     TestAddR(services, new TypeInfo[] { }, appConfig);
        
        public static IServiceCollection TestAddR(this IServiceCollection services, TypeInfo[] types,
            AppConfig appConfig)
        {
            // Resolve and add target configs
            var targetIds = services.AddWFConfiguration(appConfig).Select(x => x.Id);
            services.Configure<GenericReceiverControllerOptions>(options => { options.ConfigIds = targetIds; });
            
            // Make sure HttpClient has been added (should be idempotent)
            services.AddHttpClient();
            
            // Add File provider
            var fileProvider = new PhysicalFileProvider(Path.GetTempPath());
            services.AddSingleton<IFileProvider>(fileProvider);
            
            

            // Setup Downloading Providers and Configurations
            // TODO: Add ability to specify alternate download handler
            services.Configure<DownloadHandlerOptions>(x =>
            {
                x.DownloadJobHandlerType = typeof(DefaultDownloadHandler);
            });
            // services.AddTransient(typeof(IDownloadJobHandler), typeof(DefaultDownloadHandler));
            services.AddTransient<DefaultDownloadHandler>();

            // Setup orchestration backend (queue, broker service)
            services.AddSingleton<JobQueueChannel>();
            services.AddHostedService<DownloadBrokerService>();
            services.AddHostedService<UploadBrokerService>();
            
            
            // Setup upload providers
            services.AddTransient<OnedriveUserProvider>();
            services.AddTransient<OnedriveDriveProvider>();
            services.AddTransient<DropboxProvider>();
            services.AddTransient<SharepointProvider>();
            
            // Configure webhook receivers for specified types
            services.AddControllers().ConfigureApplicationPartManager(p =>
            {
                
                p.FeatureProviders.Add(
                    new GenericControllerFeatureProvider(types));
            });
            
            
            return services;
        }
    }

  
}