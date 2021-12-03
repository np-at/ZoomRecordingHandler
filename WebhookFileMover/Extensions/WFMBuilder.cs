using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Extensions
{
    public class WfmBuilder
    {
        internal IServiceCollection ServiceCollection;
        internal readonly string ReceiverId = Guid.NewGuid().ToString("N");

        public WfmBuilder(ref IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public WfmBuilder<T> RegisterReceiverConfig<T>(AppConfig appConfig) where T : class, new()
        {
            return new WfmBuilder<T>(ref ServiceCollection)
            {
                AppConfig = appConfig,
                ReceiverEndpointConfig = appConfig.ReceiverEndpointConfigs?.SingleOrDefault(x =>
                    x.ModelTypeName?.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? throw new KeyNotFoundException("Receiver Endpoint Config for Provided Model Type Not found")
            };
        }
    }
}