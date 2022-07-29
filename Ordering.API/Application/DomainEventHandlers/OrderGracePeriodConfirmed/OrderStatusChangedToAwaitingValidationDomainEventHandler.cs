using MediatR;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.Domain.AggregatesModel.BuyerAggregate;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.OrderGracePeriodConfirmed
{
    public class OrderStatusChangedToAwaitingValidationDomainEventHandler :
        INotificationHandler<OrderStatusChangedToWaitingValidationDomainEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBuyerRepository _buyerRepository;
        private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
        public OrderStatusChangedToAwaitingValidationDomainEventHandler(IOrderRepository orderRepository, ILoggerFactory loggerFactory, IBuyerRepository buyerRepository, IOrderingIntegrationEventService orderingIntegrationEventService)
        {
            _orderRepository = orderRepository;
            _loggerFactory = loggerFactory;
            _buyerRepository = buyerRepository;
            _orderingIntegrationEventService = orderingIntegrationEventService;
        }

        public async Task Handle(OrderStatusChangedToWaitingValidationDomainEvent orderStatusChangedToAwaitingValidationDomainEvent, CancellationToken cancellationToken)
        {
            _loggerFactory.CreateLogger<OrderStatusChangedToAwaitingValidationDomainEventHandler>()
                .LogTrace("Order with Id: {OrderId} has been successfully updated to status {Status} {Id}",
                orderStatusChangedToAwaitingValidationDomainEvent.OrderId, nameof(OrderStatus.AwaitingValidation), OrderStatus.AwaitingValidation.Id);
            var order = await _orderRepository.GetAsync(orderStatusChangedToAwaitingValidationDomainEvent.OrderId);
            var buyer = await _buyerRepository.FindByIdAsync(order.GetBuyerId.Value.ToString());
            var orderStockList = orderStatusChangedToAwaitingValidationDomainEvent.OrderItems.Select(orderItem =>
            new OrderStockItem(orderItem.ProductId, orderItem.GetUnits()));
            var orderStatusCahngedToAwaitingValidationIntegrationEvent =
                 new OrderStatusChangedToAwaitingValidationIntegrationEvent(
                     order.Id, order.OrderStatus.Name, buyer.Name, orderStockList);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStatusCahngedToAwaitingValidationIntegrationEvent);
        }
    }
}
