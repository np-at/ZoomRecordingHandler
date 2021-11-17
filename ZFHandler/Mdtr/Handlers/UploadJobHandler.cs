using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using ZFHandler.Models;
using ZFHandler.Models.ConfigurationSchemas;

namespace ZFHandler.Mdtr.Handlers
{
   public class UploadJobHandler<T> : INotificationHandler<UploadJobSpec<T>> where T : BaseClientConfig
   {
      private readonly ILogger<UploadJobHandler<T>> _logger;
      public UploadJobHandler(ILogger<UploadJobHandler<T>> logger)
      {
         _logger = logger;
      }
      public async Task Handle(UploadJobSpec<T> notification, CancellationToken cancellationToken)
      {
         _logger.LogError("Received UploadJob: {@UploadJob}", notification);
      }
   }
}