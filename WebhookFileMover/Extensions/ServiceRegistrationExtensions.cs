using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using WebhookFileMover.BackgroundServices;
using WebhookFileMover.Channels;
using WebhookFileMover.Controllers;
using WebhookFileMover.Helpers;
using WebhookFileMover.Models.Configurations;
using WebhookFileMover.Models.Configurations.Internal;
using WebhookFileMover.Services;

namespace WebhookFileMover.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        internal static readonly List<ResolvedUploadTarget> UploadTargets = new();
        internal static string? RouteTemplate { get; set; } 
        internal static TypeInfo[] Types { get; set; } = Array.Empty<TypeInfo>();
        internal static ConcurrentDictionary<Type, string> EndpointMapping { get; } = new();

        /// <summary>
        /// Adds singleton and hosted services for WebhookFileMover application features.
        /// Should be called after all receivers have been registered.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <exception cref="Exception"></exception>
        public static void FinalizeWebhookFileMoverRegistrations(
            this IServiceCollection serviceCollection)
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


            serviceCollection.FinalizeResolvedUploadTargets();
            
            
            serviceCollection.AddHostedService<UploadBrokerService>();


            serviceCollection.AddHostedService<NotificationBrokerService>();
            
            
            serviceCollection.TryAddTransient<TemplateResolverService>();
            
            serviceCollection.TryAddTransient<INotificationEvaluator, NotificationEvaluatorService>();
            // Configure webhook receivers for specified types
            serviceCollection.AddControllers().ConfigureApplicationPartManager(p => p.FeatureProviders.Add(
                new GenericControllerFeatureProvider(ServiceRegistrationExtensions.Types)));
        }

        public static WfmBuilder InitializeReceiverBuilder(this IServiceCollection serviceCollection) =>
            new(ref serviceCollection);
    }
}