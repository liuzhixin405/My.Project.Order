namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToStockConfirmedIntegrationEvent:BaseIntegrationEvent
    {
        public OrderStatusChangedToStockConfirmedIntegrationEvent(int orderId, string orderStatus, string buyerName) : base(orderId, orderStatus, buyerName)
        {

        }
    }
}
