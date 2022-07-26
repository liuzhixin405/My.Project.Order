namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToSubmittedIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToSubmittedIntegrationEvent(int orderId, string orderStatus, string buyerName) : base(orderId, orderStatus, buyerName)
        {

        }
    }
}
