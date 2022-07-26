using EventBus.Events;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public record BaseIntegrationEvent:IntegrationEvent
    {
        public int OrderId { get; }
        public string OrderStatus { get; }
        public string BuyerName { get; }

        protected BaseIntegrationEvent(int orderId, string orderStatus, string buyerName)
        {
            OrderId = orderId;
            OrderStatus = orderStatus;
            BuyerName = buyerName;
        }
    }
}
