using MediatR;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.OrderStartedEvent
{
    public class SendEmailToCustomerWhenOrderStartedDomainEventHandler:INotificationHandler<OrderStartedDomainEvent>
    {
        public SendEmailToCustomerWhenOrderStartedDomainEventHandler()
        {
            
        }

        public Task Handle(OrderStartedDomainEvent notification, CancellationToken cancellationToken)
        {
            //SEND EMAIL
            throw new NotImplementedException();
        }
    }
}
