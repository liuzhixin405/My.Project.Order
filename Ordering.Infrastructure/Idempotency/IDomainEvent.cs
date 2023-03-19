using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Idempotency
{
    public record IDomainEvent:INotification
    {
       public int Id { get; set; }
    }
    public interface IDomainEventHandler<T> where T : IDomainEvent
    {
        Task Handle(T data,CancellationToken cancellationToken);
    }
}
