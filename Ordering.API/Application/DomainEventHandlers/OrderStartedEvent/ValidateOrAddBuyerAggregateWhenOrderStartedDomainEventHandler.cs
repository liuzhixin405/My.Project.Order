using MediatR;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.API.Infrastructure.Services;
using Ordering.Domain.AggregatesModel.BuyerAggregate;
using Ordering.Domain.Events;

namespace Ordering.API.Application.DomainEventHandlers.OrderStartedEvent
{
    public class ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler : INotificationHandler<OrderStartedDomainEvent>
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IBuyerRepository buyerRepository;
        private readonly IIdentityService identityService;
        private readonly IOrderingIntegrationEventService orderingIntegartionEventService;
        public ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler(ILoggerFactory loggerFactory, IBuyerRepository buyerRepository, IIdentityService identityService, IOrderingIntegrationEventService orderingIntegartionEventService)
        {
            this.loggerFactory = loggerFactory;
            this.buyerRepository = buyerRepository;
            this.identityService = identityService;
            this.orderingIntegartionEventService = orderingIntegartionEventService;
        }

        public async Task Handle(OrderStartedDomainEvent orderStartedEvent, CancellationToken cancellationToken)
        {
            var cardTypeId = (orderStartedEvent.CardTypeId!=0)? orderStartedEvent.CardTypeId:1;
            var buyer = await buyerRepository.FindAsync(orderStartedEvent.UserId);
            bool buyerOriginallyExissted = (buyer == null) ? false : true;
            if(!buyerOriginallyExissted)
            {
                buyer = new Buyer(orderStartedEvent.UserId, orderStartedEvent.UserName);
            }
            buyer.VerifyOrAddPaymentMethod(cardTypeId,$"Payment Method on {DateTime.UtcNow}",
                  orderStartedEvent.CardNumber,
                                           orderStartedEvent.CardSecurityNumber,
                                           orderStartedEvent.CardHolderName,
                                           orderStartedEvent.CardExpiration,
                                           orderStartedEvent.Order.Id);
            var buyerUpdated = buyerOriginallyExissted ? buyerRepository.Update(buyer) : buyerRepository.Add(buyer);
            await buyerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            var orderStatusChangedToSubmittedIntegrationEvent = new OrderStatusChangedToSubmittedIntegrationEvent(orderStartedEvent.Order.Id, orderStartedEvent.Order.OrderStatus.Name, buyer.Name);
            await orderingIntegartionEventService.AddAndSaveEventAsync(orderStatusChangedToSubmittedIntegrationEvent);
            loggerFactory.CreateLogger<ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler>()
               .LogTrace("Buyer {BuyerId} and related payment method were validated or updated for orderId: {OrderId}.",
                   buyerUpdated.Id, orderStartedEvent.Order.Id);
        }
    }
}
