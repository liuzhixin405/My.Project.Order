using EventBus.Events;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToCancelledIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToCancelledIntegrationEvent(int orderId, string orderStatus, string buyerName) : base(orderId, orderStatus, buyerName)
        {

        }
    }
}
