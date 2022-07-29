using MediatR;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.Domain.AggregatesModel.BuyerAggregate;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.OrderPaid
{
    public class OrderStatusChangedToPaidDomainEventHandler : NotificationHandler<OrderStatusChangedToPaidDomainEvent>
    {
        private readonly IOrderRepository orderRepository;
        private readonly ILoggerFactory loggerFactory;
        private readonly IBuyerRepository buyerRepository;
        private readonly IOrderingIntegrationEventService orderingIntegrationEventService;
        public OrderStatusChangedToPaidDomainEventHandler(IOrderRepository orderRepository, ILoggerFactory loggerFactory, IBuyerRepository buyerRepository, IOrderingIntegrationEventService orderingIntegrationEventService)
        {
            this.orderRepository = orderRepository;
            this.loggerFactory = loggerFactory;
            this.buyerRepository = buyerRepository;
            this.orderingIntegrationEventService = orderingIntegrationEventService;
        }

        protected override async void Handle(OrderStatusChangedToPaidDomainEvent message)
        {
            loggerFactory.CreateLogger<OrderStatusChangedToPaidDomainEventHandler>()
               .LogTrace("Order with Id: {OrderId} has been successfully updated to status {Status} ({Id})",
                   message.OrderId, nameof(OrderStatus.Paid), OrderStatus.Paid.Id);
            var order = await orderRepository.GetAsync(message.OrderId);
            var buyer = await buyerRepository.FindByIdAsync(order.GetBuyerId.Value.ToString());

            var orderStockList = message.OrderItems
               .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.GetUnits()));
            var orderStatusChangedToPaidIntegrationEvent = new OrderStatusChangedToPaidIntegrationEvent(
              message.OrderId,
              order.OrderStatus.Name,
              buyer.Name,
              orderStockList);

            await orderingIntegrationEventService.AddAndSaveEventAsync(orderStatusChangedToPaidIntegrationEvent);

        }
    }
}
