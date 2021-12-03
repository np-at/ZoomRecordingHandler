using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebhookFileMover.BackgroundServices;
using WebhookFileMover.Controllers;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Models.Interfaces;
using WebhookFileMover.Providers.Download;
using WebhookFileMover.Providers.Dropbox;
using WebhookFileMover.Providers.Notifications;
using WebhookFileMover.Providers.OneDrive;

namespace WebhookFileMover.Extensions
{
    public class WFMBuilder<T> : WFMBuilder
    {
        public WFMBuilder(ref IServiceCollection serviceCollection) : base(ref serviceCollection)
        {
        }
        internal AppConfig? AppConfig;

        internal ReceiverEndpointConfig? ReceiverEndpointConfig { get; set; }

        public WFMBuilder<T> RegisterEndpointFromConfig()
        {
            
                if (!string.IsNullOrWhiteSpace(ReceiverEndpointConfig?.RouteSuffix))
                    if (!ServiceRegistrationExtensions.EndpointMapping.TryAdd(typeof(T), ReceiverEndpointConfig.RouteSuffix))
                        throw new Exception("Error while attempting to add type to Custom Endpoint Mapping");
                // else
                //     if (!ServiceRegistrationExtensions.EndpointMapping.TryAdd(typeof(T), endpoint))
                //         throw new Exception("Error while attempting to add type to Custom Endpoint Mapping");
            return this;
        }

        public WFMBuilder<T> RegisterWebhookTransformer<TY>() where TY : class, IWebhookDownloadJobTransformer<T>
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
            // var targetIds = ServiceCollection.AddWFConfiguration(AppConfig).Select(x => x.Id);
            RegisterReceiverConfigurations();
            RegisterNotificationProviders();
            var targets = this.ResolveTargets();
            var targetIds = targets.Select(x => x.Id);
            ServiceRegistrationExtensions.UploadTargets.AddRange(targets);

            ServiceCollection.Configure<GenericReceiverControllerOptions<T>>(options =>
            {
                options.ConfigIds = targetIds;
                options.AssociatedReceiverId = _receiverId;
            });
            
            if (!string.IsNullOrWhiteSpace(AppConfig?.BaseReceiverConfig?.BaseRouteTemplate) && string.IsNullOrWhiteSpace(ServiceRegistrationExtensions.RouteTemplate))
                ServiceRegistrationExtensions.RouteTemplate ??= AppConfig.BaseReceiverConfig?.BaseRouteTemplate;
            
            
            
            ServiceRegistrationExtensions.Types =
                ServiceRegistrationExtensions.Types.Append(typeof(T).GetTypeInfo()).ToArray();
        }

        private void RegisterReceiverConfigurations()
        {
            ServiceCollection.Configure<ResolvedReceiverConfiguration>(_receiverId, config =>
            {
                config.ResolvedUploadTargets = ResolveTargets();
                config.NotificationProviderConfigs = ReceiverEndpointConfig?.NotificationProviderConfigs ??  Array.Empty<NotificationProviderConfig>();
                config.Id = _receiverId;
            });
        }

        private void RegisterNotificationProviders()
        {
            if (ReceiverEndpointConfig?.NotificationProviderConfigs.Any(x=>x.ProviderType == NotificationProviderType.SlackBot) ?? false)
                ServiceCollection.TryAddTransient<SlackNotificationProvider>();
        }
        private List<ResolvedUploadTarget> ResolveTargets()
        {
            var targets = new List<ResolvedUploadTarget>();
            foreach (string associatedUploadTargetId in ReceiverEndpointConfig?.AssociatedUploadTargetIds ?? Array.Empty<string>())
            {
                var associatedUploadTarget = AppConfig?.UploadTargets?.SingleOrDefault(x =>
                    x.Name?.Equals(associatedUploadTargetId, StringComparison.InvariantCultureIgnoreCase) ?? false);

                if (associatedUploadTarget == null)
                    throw new KeyNotFoundException(
                        $"Associated Upload Target not found in ReceiverEndpointConfig {ReceiverEndpointConfig} for AssociatedUploadTargetId of {associatedUploadTargetId}");
                var associatedUploadConfig =
                    AppConfig?.UploadConfigs?.SingleOrDefault(x => x.Identifier?.Equals(associatedUploadTarget?.ConfigId)??false) ?? throw new KeyNotFoundException();
                targets.Add(new ResolvedUploadTarget()
                {
                    UploadTarget = associatedUploadTarget,
                    UploadTargetConfig = associatedUploadConfig,
                    NotificationProviderConfig = ReceiverEndpointConfig?.NotificationProviderConfigs ?? Array.Empty<NotificationProviderConfig>()
                });
                
            }

            return targets;
        }
    }
    //
    // public class ReceiverBldr<T>
    // {
    //     internal IServiceCollection _serviceCollection;
    //     internal TplAsyncPipeline<T, DownloadJobBatch>? DlBatchPipelineBuilderipelineBuilder;
    //     internal IEnumerable<ResolvedUploadTarget> _uploadTargets;
    //
    //     public ReceiverBldr(IServiceCollection serviceCollection)
    //     {
    //         _serviceCollection = serviceCollection;
    //     }
    //
    //     private void AddConfigs(AppConfig appConfig)
    //     {
    //         _uploadTargets = ConfigParser.ResolveTargets(appConfig);
    //     }
    //
    //     public void AddTransformer(Func<T, IEnumerable<DownloadJob>> transformerFunc)
    //     {
    //         var builder = new TplAsyncPipelineBuilder<T, DownloadJobBatch>();
    //         DlBatchPipelineBuilderipelineBuilder = builder.AddStep(transformerFunc)
    //             .AddStep(jobs => new DownloadJobBatch(jobs: jobs)).Build();
    //     }
    //
    //     public void Build()
    //     {
    //     }
    // }

    // public static class Builders
    // {
    //     public static ReceiverBldr<T> AddReceiver<T>(this IServiceCollection serviceCollection, AppConfig config) =>
    //         new(serviceCollection)
    //         {
    //             _uploadTargets = ConfigParser.ResolveTargets(config)
    //         };
    //     // public static IServiceCollection TestAddR(this IServiceCollection services, AppConfig appConfig) =>
    //     //     TestAddR(services, new TypeInfo[] { }, appConfig);
    //
    //     public static IServiceCollection TestAddR(this IServiceCollection services, TypeInfo[] types,
    //         AppConfig appConfig)
    //     {
    //         // Resolve and add target configs
    //         // var targetIds = services.AddWFConfiguration(appConfig).Select(x => x.Id);
    //         // services.Configure<GenericReceiverControllerOptions>(options => { options.ConfigIds = targetIds; });
    //
    //         // Make sure HttpClient has been added (should be idempotent)
    //         services.AddHttpClient();
    //
    //         // Add File provider
    //         var fileProvider = new PhysicalFileProvider(Path.GetTempPath());
    //         services.AddSingleton<IFileProvider>(fileProvider);
    //
    //
    //         // Setup Downloading Providers and Configurations
    //         // TODO: Add ability to specify alternate download handler
    //         services.Configure<DownloadHandlerOptions>(x =>
    //         {
    //             x.DownloadJobHandlerType = typeof(DefaultDownloadHandler);
    //         });
    //         // services.AddTransient(typeof(IDownloadJobHandler), typeof(DefaultDownloadHandler));
    //         services.AddTransient<DefaultDownloadHandler>();
    //
    //         // Setup orchestration backend (queue, broker service)
    //         services.AddSingleton<JobQueueChannel>();
    //         services.AddHostedService<DownloadBrokerService>();
    //         services.AddHostedService<UploadBrokerService>();
    //
    //
    //         // Setup upload providers
    //         services.AddTransient<OnedriveUserProvider>();
    //         services.AddTransient<OnedriveDriveProvider>();
    //         services.AddTransient<DropboxProvider>();
    //         services.AddTransient<SharepointProvider>();
    //
    //         // Configure webhook receivers for specified types
    //         services.AddControllers().ConfigureApplicationPartManager(p =>
    //         {
    //             p.FeatureProviders.Add(
    //                 new GenericControllerFeatureProvider(types));
    //         });
    //
    //
    //         return services;
    //     }
    // }
}