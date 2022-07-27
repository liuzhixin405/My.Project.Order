using EventBus.Events;

namespace Ordering.API.Application.IntegrationEvents
{
    public interface IOrderingIntegrationEventService
    {
        Task PublishEventsThroughEventBusAsync(Guid transactinonId);
        Task AddAndSaveEventAsync(IntegrationEvent @event);
    }
}
