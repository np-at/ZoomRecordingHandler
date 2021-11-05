using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace ZoomFileManager.Extensions
{
    public static class ServicesExtension
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder ConfigureUploadServiceClient(this IHttpClientBuilder builder, Action<HttpClient> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.HttpClientActions.Add(configureClient));

            return builder;
        }
        public static IHttpClientBuilder AddUploadService(this IServiceCollection services, string name, Action<HttpClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddUploadServiceClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureUploadServiceClient(configureClient);
            return builder;
        }
        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddUploadServiceClient(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging();
            services.AddOptions();

            //
            // Core abstractions
            //
            services.TryAddTransient<HttpMessageHandlerBuilder, DefaultHttpMessageHandlerBuilder>();
            services.TryAddSingleton<DefaultHttpClientFactory>();
            services.TryAddSingleton<IHttpClientFactory>(serviceProvider => serviceProvider.GetRequiredService<DefaultHttpClientFactory>());
            services.TryAddSingleton<IHttpMessageHandlerFactory>(serviceProvider => serviceProvider.GetRequiredService<DefaultHttpClientFactory>());

            //
            // Typed Clients
            //
            services.TryAdd(ServiceDescriptor.Transient(typeof(ITypedHttpClientFactory<>), typeof(DefaultTypedHttpClientFactory<>)));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(DefaultTypedHttpClientFactory<>.Cache), typeof(DefaultTypedHttpClientFactory<>.Cache)));

            //
            // Misc infrastructure
            //
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, LoggingHttpMessageHandlerBuilderFilter>());

            // This is used to track state and report errors **DURING** service registration. This has to be an instance
            // because we access it by reaching into the service collection.
            services.TryAddSingleton(new HttpClientMappingRegistry());

            // Register default client as HttpClient
            services.TryAddTransient(s => s.GetRequiredService<IHttpClientFactory>().CreateClient(string.Empty));

            return services;
        }
        public static IHttpClientBuilder AddUploadServiceClient(this IServiceCollection services, string name)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddUploadServiceClient(services);

            return new DefaultHttpClientBuilder(services, name);
        }
    }
}