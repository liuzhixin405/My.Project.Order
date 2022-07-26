using EventBus.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public class OrderStatusChangedToShippedIntegrationEventHandler:IIntegrationEventHandler<OrderStatusChangedToShippedIntegrationEvent>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderStatusChangedToShippedIntegrationEventHandler> _logger;

        public OrderStatusChangedToShippedIntegrationEventHandler(
            IHubContext<NotificationHub> hubContext,
            ILogger<OrderStatusChangedToShippedIntegrationEventHandler> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task Handle(OrderStatusChangedToShippedIntegrationEvent @event)
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
