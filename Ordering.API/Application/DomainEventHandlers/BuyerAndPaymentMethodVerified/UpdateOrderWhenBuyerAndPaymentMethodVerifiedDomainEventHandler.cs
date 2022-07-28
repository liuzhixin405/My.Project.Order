using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.BuyerAndPaymentMethodVerified
{
    public class UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler : INotificationHandler<BuyerAndPaymentMethodVerifiedDomainEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _logger;
        public UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler(IOrderRepository orderRepository, ILoggerFactory logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Handle(BuyerAndPaymentMethodVerifiedDomainEvent @event, CancellationToken cancellationToken)
        {
            var orderToUpdate = await _orderRepository.GetAsync(@event.OrderId);
            orderToUpdate.SetBuyerId(@event.Buyer.Id);
            orderToUpdate.SetPaymentId(@event.Payment.Id);
            _logger.CreateLogger<UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler>()
                .LogTrace("Order with Id: {OrderId} has been successfully updated with a payment method {PaymentMethod} ({Id})",
                    @event.OrderId, nameof(@event.Payment), @event.Payment.Id);
        }
    }
}
