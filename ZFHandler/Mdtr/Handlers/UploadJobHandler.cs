using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ZFHandler.Models;

namespace ZFHandler.Mdtr.Handlers
{
   public abstract class UploadJobHandler<T> : INotificationHandler<UploadJobSpec<T>>
   {
      
      public abstract Task Handle(UploadJobSpec<T> notification, CancellationToken cancellationToken);

   }
}