namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToPaidIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToPaidIntegrationEvent(int orderId, string orderStatus, string buyerName) : base(orderId, orderStatus, buyerName)
        {

        }
    }
}
