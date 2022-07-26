using EventBus.Events;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToAwaitingValidationIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToAwaitingValidationIntegrationEvent(int orderId,string orderStatus,string buyerName):base(orderId,orderStatus,buyerName)
        {

        }
    }
}
