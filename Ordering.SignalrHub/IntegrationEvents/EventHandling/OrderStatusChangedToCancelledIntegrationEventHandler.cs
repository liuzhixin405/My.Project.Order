using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public class OrderStatusChangedToCancelledIntegrationEventHandler:IIntegrationEventHandler<OrderStatusChangedToCancelledIntegrationEvent>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderStatusChangedToCancelledIntegrationEventHandler> _logger;
        public OrderStatusChangedToCancelledIntegrationEventHandler(IHubContext<NotificationHub> hubContext, ILogger<OrderStatusChangedToCancelledIntegrationEventHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(OrderStatusChangedToCancelledIntegrationEvent @event)
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
