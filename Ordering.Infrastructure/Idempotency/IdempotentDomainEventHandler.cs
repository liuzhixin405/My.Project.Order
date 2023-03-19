using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Idempotency
{
    public class IdempotentDomainEventHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent>
        where TDomainEvent : IDomainEvent //第二种幂等性校验
    {
        private readonly INotificationHandler<TDomainEvent> _decorated;
        private readonly OrderingContext _orderingContext;
        public IdempotentDomainEventHandler(INotificationHandler<TDomainEvent> decorated,
            OrderingContext orderingContext)
        {
            _decorated = decorated;
            _orderingContext = orderingContext;
        }
        public async Task Handle(TDomainEvent data, CancellationToken cancellationToken)
        {
            string consumer = _decorated.GetType().Name;
            if (await _orderingContext.Set<OutboxMessageConsumer>().AnyAsync(o => o.Id == data.Id && o.Name == consumer,cancellationToken))
            {
                return;
            }
             await _decorated.Handle(data, cancellationToken);
            _orderingContext.Set<OutboxMessageConsumer>().Add(new OutboxMessageConsumer
            {
                Id=data.Id,
                Name=consumer
            });
            await _orderingContext.SaveChangesAsync(cancellationToken);
        }
    }

    public class OutboxMessageConsumer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
