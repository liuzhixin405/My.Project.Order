using EventBus.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public class OrderStatusChangedToPaidIntegrationEventHandler:IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderStatusChangedToPaidIntegrationEventHandler> _logger;
        public OrderStatusChangedToPaidIntegrationEventHandler(IHubContext<NotificationHub> hubContext,ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
        {
            using (LogContext.PushProperty("IntegrationEventContext", $"{@event.Id}-{Program.AppName}"))
            {
                _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, Program.AppName, @event);

                await _hubContext.Clients
                    .Group(@event.BuyerName)
                    .SendAsync("UpdatedOrderState", new { OrderId = @event.OrderId, Status = @event.OrderStatus });
            }
        }
    }
}
