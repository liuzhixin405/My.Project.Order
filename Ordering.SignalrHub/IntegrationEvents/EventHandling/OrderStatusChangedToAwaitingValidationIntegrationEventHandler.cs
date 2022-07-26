using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace Ordering.SignalrHub.IntegrationEvents
{
    public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler : IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
    {
        protected readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler> _logger;
        public OrderStatusChangedToAwaitingValidationIntegrationEventHandler(IHubContext<NotificationHub> hubContext, ILogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler> logger)
        {
            _hubContext = hubContext??throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent @event)
        {
            using(LogContext.PushProperty("IntegrationEventContext", $"{@event.Id}-{Program.AppName}"))
            {
                _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, Program.AppName, @event);

                await _hubContext.Clients.Group(@event.BuyerName).SendAsync("UpdateOrderState", new { OrderId = @event.OrderId, Status = @event.OrderStatus }); ;
            }
        }
    }
}
