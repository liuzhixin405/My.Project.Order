using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Idempotency
{
    /// <summary>
    ///第二种幂等性校验需要 mediator的event继承IDomainEvent不直接继承INotification
    /// </summary>
    public record IDomainEvent:INotification
    {
       public int Id { get; set; }
    }
    public interface IDomainEventHandler<T> where T : IDomainEvent
    {
        Task Handle(T data,CancellationToken cancellationToken);
    }
}
