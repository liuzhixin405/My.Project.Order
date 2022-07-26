using EventBus.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public record OrderStatusChangedToStockConfirmedIntegrationEventHandler : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> _logger;

        public OrderStatusChangedToStockConfirmedIntegrationEventHandler(
            IHubContext<NotificationHub> hubContext,
            ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event)
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
