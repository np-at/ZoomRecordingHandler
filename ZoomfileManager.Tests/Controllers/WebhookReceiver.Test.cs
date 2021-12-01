using Serilog.Extensions.Logging;
using Xunit;
using ZoomFileManager.Controllers;

namespace ZoomFileManager.Tests.Controllers
{
    public class WebhookReceiverTests
    {
        [Fact]
        public void Test_WebhookReceiver()
        {
            var opts = new WebhookReceiversOptions()
            {
                AllowedTokens = System.Array.Empty<string>(),
            };
            var loggerFactory = new SerilogLoggerFactory();

            // var controller = new WebhookReceiver(new Logger<WebhookReceiver>(loggerFactory),
            //     new OptionsWrapper<WebhookReceiversOptions>(new WebhookReceiversOptions()
            //     ), new ProcessingChannel(new Logger<ProcessingChannel>(loggerFactory)));
        }
    }
}