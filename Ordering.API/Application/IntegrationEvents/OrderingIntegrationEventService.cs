using EventBus.Abstractions;
using EventBus.Events;
using IntegrationEventLogEF;
using IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Ordering.Infrastructure;
using System.Data.Common;

namespace Ordering.API.Application.IntegrationEvents
{
    public class OrderingIntegrationEventService : IOrderingIntegrationEventService
    {
        private readonly Func<DbConnection, IIntegrationEventLogService> _integrationEventLogServiceFactory;
        private readonly IEventBus _eventBus;
        private readonly OrderingContext _orderingContext;
        private readonly IIntegrationEventLogService _eventLogService;
        private readonly ILogger<OrderingIntegrationEventService> _logger;

        public OrderingIntegrationEventService(IEventBus eventBus, OrderingContext orderingContext, IntegrationEventLogContext integrationlogcontext, Func<DbConnection, IIntegrationEventLogService> integrationEventLogServiceFactory, ILogger<OrderingIntegrationEventService> logger)
        {
            _integrationEventLogServiceFactory = integrationEventLogServiceFactory;
            _eventBus = eventBus;
            _orderingContext = orderingContext;
            _eventLogService = _integrationEventLogServiceFactory(_orderingContext.Database.GetDbConnection());
            _logger = logger;
        }

        public async Task AddAndSaveEventAsync(IntegrationEvent evt)
        {
            _logger.LogInformation("----- Enqueuing integration event {IntegrationEventId} to repository ({@IntegrationEvent})", evt.Id, evt);
            await _eventLogService.SaveEventAsync(evt,_orderingContext.GetCurrentTransaction());

        }

        public async Task PublishEventsThroughEventBusAsync(Guid transactinonId)
        {
            var pendingLogEvents = await _eventLogService.RetrieveEventLogsPendingToPublishAsync(transactinonId);
            foreach (var logEvt in pendingLogEvents)
            {
                _logger.LogInformation("----- Publishing integration event: {IntegrationEventId} from {AppName} - ({@IntegrationEvent})", logEvt.EventId, Program.AppName, logEvt.IntegrationEvent);

                try
                {
                    await _eventLogService.MarkEventAsInProgressAsync(logEvt.EventId);
                    _eventBus.Publish(logEvt.IntegrationEvent);
                    await _eventLogService.MarkEventAsPublishedAsynn(logEvt.EventId);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "ERROR publishing integration event: {IntegrationEventId} from {AppName}", logEvt.EventId, Program.AppName);
                    await _eventLogService.MarkEventAsFailedAsync(logEvt.EventId);
                }
            }
        }
    }
}
