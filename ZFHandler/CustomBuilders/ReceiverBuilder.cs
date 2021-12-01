using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using ZFHandler.Controller;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebhookFileMover.Models.Configurations;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Pipelines.TPL;
using ZFHandler.Helpers;
using ZFHandler.Mdtr.Commands;
using ZFHandler.Mdtr.Handlers;
using ZFHandler.Models;
using ZFHandler.Pipelines.Impl;
using ZFHandler.Services;
using ZFHandler.Services.BaseProviderImplementations.UploadServices;
using ZoomFileManager.Models;

namespace ZFHandler.CustomBuilders
{
    // public class WebhookConfig<T> where T : IValidator<T>, IDownloadService<T>
    // {
    //     public string Identifier { get; set; }
    //     public string Route { get; set; }
    //     public IValidator<T> Implementer { get; }
    // }
    public class RBuilder
    {
        internal IServiceCollection _serviceCollection;

        public RBuilder(ref IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public RBuilder<T> RegisterWebhookType<T>(
            Func<T, CancellationToken?, ValueTask<IEnumerable<DownloadJob>>> transformer, bool debug = false)
            where T : class, IRConv<T>, new()

        {
            _serviceCollection.Configure<TransformerOption<T>>(option => option.TransformFunction = transformer);
            _serviceCollection
                .AddTransient<IRequestHandler<T, DownloadJobBatch>, ReceiverTransformHandler<T>>();
            if (debug)
                _serviceCollection.AddTransient(typeof(IPipelineBehavior<T, T>), typeof(MediatRLoggingBehaviour<T, T>));
            return new RBuilder<T>(ref _serviceCollection);
        }
    }

    public class RBuilder<T> : RBuilder
        where T : class, IRConv<T>, new()
    {
        internal ReceiverConfig[]? _receiverConfigs;

        internal string? _receiverEndpoint;

        internal List<UploadTarget> _uploadTargets = new();
        private List<UploadTargetConfig> _uploadTargetConfigs = new();

        public RBuilder(ref IServiceCollection serviceCollection) : base(ref serviceCollection)
        {
        }

        public RBuilder<T> AddReceiver(Action<ReceiverConfig> receiverConfig)
        {
            if (!String.IsNullOrEmpty(_receiverEndpoint))
                throw new ArgumentException("Receiver Endpoint can only be specified once per config flow");
            var config = new ReceiverConfig();
            receiverConfig?.Invoke(config);
            return this;
        }


        public RBuilder<T> RegisterDefaultDownloader(
            Action<DownloadJobHandlerOptions<DownloadJobHandler>>? handlerOptions = null) =>
            RegisterDownloadHandler<DownloadJobHandler>(o => handlerOptions?.Invoke(o));


        public RBuilder<T> RegisterDownloadHandler<TY>(Action<DownloadJobHandlerOptions<TY>>? handlerOptions = null)
            where TY : class, IRequestHandler<DownloadJob, FileInfo?>
        {
            _serviceCollection.Configure<DownloadJobHandlerOptions<TY>>(o => handlerOptions?.Invoke(o));
            _serviceCollection.TryAddTransient<IRequestHandler<DownloadJob, FileInfo?>, TY>();

            return this;
        }

        public RBuilder<T> RegisterClientConfig(UploadTargetConfig uploadTargetConfig)
        {
            _uploadTargetConfigs.Add(uploadTargetConfig);
            return this;
        }

        public RBuilder<T> RegisterClientConfigs(IEnumerable<UploadTargetConfig> uploadTargetConfigs)
        {
            _uploadTargetConfigs.AddRange(uploadTargetConfigs);
            return this;
        }

        public RBuilder<T> RegisterUploadTarget(UploadTarget uploadTarget)
        {
            _uploadTargets.Add(uploadTarget);
            return this;
        }

        public RBuilder<T> AddMediatrLogging()
        {
            _serviceCollection.TryAddTransient(typeof(IPipelineBehavior<T, T>), typeof(MediatRLoggingBehaviour<T, T>));
            return this;
        }

        public RBuilder<T> RegisterDefaultUploadClients()
        {
            return this;
        }


        public void Build()
        {
            // TODO: REMOVE THIS, FOR TESTING ONLY
            IEnumerable<Type> types = new[] { typeof(T) };

            _serviceCollection.AddControllers(options =>
                    options.Conventions.Add(new GenericControllerRouteConvention()))
                .ConfigureApplicationPartManager(
                    m => m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider(types)));
            ServiceRegistrar.AddRequiredServices(_serviceCollection, new MediatRServiceConfiguration());
            // _serviceCollection.TryAddSingleton<INotificationHandler<DownloadJobBatch>, BrokerService>();

            foreach (var uploadTarget in _uploadTargets)
            {
                var conf = _uploadTargetConfigs.Where(x => x.Identifier == uploadTarget.ConfigId);
                var uploadTargetConfigs = conf as UploadTargetConfig[] ?? conf.ToArray();
                if (uploadTargetConfigs.Length > 1)
                    throw new ArgumentOutOfRangeException(nameof(uploadTarget.ConfigId),
                        $"Multiple Upload Client configurations found for specified Config Id of {uploadTarget.ConfigId}");
                if (!uploadTargetConfigs.Any())
                    throw new ArgumentOutOfRangeException(nameof(uploadTarget.ConfigId),
                        $"No Upload client configuration found for specified config id of {uploadTarget.ConfigId}");
                var c = uploadTargetConfigs.FirstOrDefault() ?? throw new ArgumentNullException();
                switch (c.Type)
                {
                    case JobType.Sharepoint:
                        _serviceCollection.Configure<SharepointProviderConfig<T>>(o =>
                        {
                            o.ClientId = c.ClientConfig?.ClientId;
                        });

                        _serviceCollection
                            .TryAddTransient<INotificationHandler<UploadJobSpec<T>>, SharepointProvider<T>>();
                        break;
                    case JobType.OnedriveUser:
                        // _serviceCollection.Configure<OnedriveUserProviderOptions<T>>();
                        _serviceCollection
                            .TryAddTransient<INotificationHandler<UploadJobSpec<T>>, OnedriveUserProvider<T>>();
                        break;
                    case JobType.OnedriveDrive:
                        // _serviceCollection.Configure<OnedriveDriveProviderOptions<T>>();
                        _serviceCollection
                            .TryAddTransient<INotificationHandler<UploadJobSpec<T>>, OnedriveDriveProvider<T>>();
                        break;
                    case JobType.Dropbox:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        // public static void Build2(Func<T, IEnumerable<DownloadJob>> transformer)
        // {
        //     var builder = new DisruptorPipelineBuilder();
        //     var pipeline = builder.Build<T, IEnumerable<DownloadJob>>(transformer, 1)
        //         .AddStep(jobs => new DownloadJobBatch()
        //         {
        //             Jobs = jobs
        //         }, 1)
        //         .AddStep(batch =>
        //         {
        //         
        //         }, 1).Create();
        // }
    }

    public static class RB2
    {
        public static IServiceCollection AddRb3Test<T>(this IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApplicationPartManager(p => p.FeatureProviders.Add(new GenericControllerFeatureProvider()));

            return services;
        }

        public static IServiceCollection AddRb2Test<T>(this IServiceCollection services,
            Func<T, CancellationToken?, ValueTask<IEnumerable<DownloadJob>>>? transformer) where T : IRConv<T>, new()
        {
            // TODO: REMOVE THIS, FOR TESTING ONLY
            IEnumerable<Type> types = new[] { typeof(T) };
            services.AddControllers(options =>
                    options.Conventions.Add(new GenericControllerRouteConvention()))
                .ConfigureApplicationPartManager(
                    m => m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider(types)));
            ServiceRegistrar.AddRequiredServices(services, new MediatRServiceConfiguration());
            services.AddSingleton<TPLFlowImplementation<T>>();

            services.AddTransient<DownloadJobHandler>();
            services.Configure<TransformerOption<T>>(option => option.TransformFunction = transformer);
            services.AddTransient<ReceiverTransformHandler<T>>();

            return services;
        }
    }

    public static class ReceiverBuilder
    {
        public static RBuilder InitializeReceiverBuilder(this IServiceCollection serviceCollection)
        {
            // serviceCollection.AddReceivers();
            var builder = new RBuilder(ref serviceCollection);
            return builder;
        }

        public static IServiceCollection AddReceivers(this IServiceCollection services, Action<AppConfig> appConfig,
            Assembly? _assembly = null)
        {
            var config = new AppConfig();
            appConfig?.Invoke(config);

            foreach (var configUploadTarget in config.UploadTargets ?? ArraySegment<UploadTarget>.Empty)
            {
                var assocConfig =
                    config.UploadConfigs?.FirstOrDefault(
                        x => x.Identifier?.Equals(configUploadTarget.ConfigId) ?? false) ??
                    throw new ArgumentOutOfRangeException(nameof(appConfig),
                        "Matching upload configuration id not found for upload target config");
                services.Configure<UploadTargetConfig>(configUploadTarget.GetHashCode().ToString(), f =>
                {
                    f.Identifier = assocConfig.Identifier;
                    f.Type = assocConfig.Type;
                    f.ClientConfig = assocConfig.ClientConfig;
                    f.RootPath = assocConfig.RootPath;
                });
            }

            services.InitializeReceiverBuilder()
                .RegisterWebhookType<Zoominput>(Zoominput.ConvertToDownloadJobAsync)
                .RegisterDefaultDownloader()
                .AddMediatrLogging()
                .Build();


            // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatRLoggingBehaviour<,>));

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


            // services
            //     .AddTransient<INotificationHandler<UploadJobSpec<SharepointClientConfig>>,
            //         UploadJobHandler<SharepointClientConfig>>();
            // services
            //     .AddTransient<INotificationHandler<UploadJobSpec<OD_DriveClientConfig>>,
            //         UploadJobHandler<OD_DriveClientConfig>>();
            // services
            //     .AddTransient<INotificationHandler<UploadJobSpec<DropBoxClientConfig>>,
            //         UploadJobHandler<DropBoxClientConfig>>();
            // services.AddTransient<UploadJobHandler<OD_UserClientConfig>, OnedriveUserProvider>();
            // services
            //     .AddTransient<INotificationHandler<UploadJobSpec<OD_UserClientConfig>>,
            //         UploadJobHandler<OD_UserClientConfig>>();


            // services.Configure<AppConfig>("test", x => x.AllowedTokens = new []{"test"});
            // services.TryAddEnumerable(config.Select(x=>ServiceDescriptor.Scoped<ControllerBase,WebReceiver<T, TY>>()));


            // services
            //     .TryAddTransient<IRequestHandler<Zoominput, DownloadJobBatch>, ReceiverTransformHandler<Zoominput>>();

            return services;
        }

        // private static void PipelineConfigurations(IServiceCollection serviceCollection, AppConfig? appConfig)
        // {
        //     var dict = new Dictionary<string, >()
        // }
    }

    public class ResolvedUploadTargets
    {
        public string Id { get; set; }
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