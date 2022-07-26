namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToShippedIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToShippedIntegrationEvent(int orderId, string orderStatus, string buyerName) : base(orderId, orderStatus, buyerName)
        {

        }
    }
}
