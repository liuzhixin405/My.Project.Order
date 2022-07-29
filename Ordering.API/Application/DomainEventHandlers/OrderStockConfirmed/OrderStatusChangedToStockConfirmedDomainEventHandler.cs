using MediatR;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.Domain.AggregatesModel.BuyerAggregate;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.OrderStockConfirmed
{
    public class OrderStatusChangedToStockConfirmedDomainEventHandler : INotificationHandler<OrderStatusChangedToStockConfirmedDomainEvent>
    {
        private readonly IOrderRepository orderRepository;
        private readonly IBuyerRepository buyerRepository;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOrderingIntegrationEventService orderingIntegrationEventService;
        public OrderStatusChangedToStockConfirmedDomainEventHandler(IOrderRepository orderRepository, IBuyerRepository buyerRepository, ILoggerFactory loggerFactory, IOrderingIntegrationEventService orderingIntegrationEventService)
        {
            this.orderRepository = orderRepository;
            this.buyerRepository = buyerRepository;
            this.loggerFactory = loggerFactory;
            this.orderingIntegrationEventService = orderingIntegrationEventService;
        }

        public async Task Handle(OrderStatusChangedToStockConfirmedDomainEvent message, CancellationToken cancellationToken)
        {
            loggerFactory.CreateLogger<OrderStatusChangedToStockConfirmedDomainEventHandler>()
              .LogTrace("Order with Id: {OrderId} has been successfully updated to status {Status} ({Id})",
                  message.OrderId, nameof(OrderStatus.StockedConfirmed), OrderStatus.StockedConfirmed.Id);

            var order = await orderRepository.GetAsync(message.OrderId);
            var buyer = await buyerRepository.FindByIdAsync(order.GetBuyerId.Value.ToString());

            var orderStatusChangedToStockConfirmedIntegrationEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(order.Id, order.OrderStatus.Name, buyer.Name);
            await orderingIntegrationEventService.AddAndSaveEventAsync(orderStatusChangedToStockConfirmedIntegrationEvent);
        }
    }
}
