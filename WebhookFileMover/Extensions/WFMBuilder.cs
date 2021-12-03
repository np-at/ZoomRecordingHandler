using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Extensions
{
    public class WFMBuilder
    {
        internal IServiceCollection ServiceCollection;
        internal string _receiverId = Guid.NewGuid().ToString("N");

        public WFMBuilder(ref IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public WFMBuilder<T> RegisterReceiverConfig<T>(AppConfig appConfig, bool debug = false)
        {
            return new WFMBuilder<T>(ref ServiceCollection)
            {
                AppConfig = appConfig,
                ReceiverEndpointConfig = appConfig?.ReceiverEndpointConfigs?.SingleOrDefault(x =>
                    x.ModelTypeName?.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? throw new KeyNotFoundException("Receiver Endpoint Config for Provided Model Type Not found")
            };
        }
    }
}