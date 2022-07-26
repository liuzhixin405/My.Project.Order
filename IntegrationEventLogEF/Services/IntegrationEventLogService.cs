using EventBus.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEventLogEF.Services
{
    public class IntegrationEventLogService : IIntegrationEventLogService, IDisposable
    {
        private readonly IntegrationEventLogContext _integrationEventLogContext;
        private readonly DbConnection _dbConnection;
        private readonly List<Type> _eventtypes;
        private volatile bool disposedValue;
        public IntegrationEventLogService(DbConnection dbConnection)
        {
            _dbConnection = dbConnection?? throw new ArgumentNullException(nameof(dbConnection));
            _integrationEventLogContext = new IntegrationEventLogContext(new DbContextOptionsBuilder<IntegrationEventLogContext>().UseSqlServer(_dbConnection).Options);

            _eventtypes = Assembly.Load(Assembly.GetEntryAssembly().FullName).GetTypes().Where(x => x.Name.EndsWith(nameof(IntegrationEvent))).ToList();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if(disposing)
                    _integrationEventLogContext?.Dispose();
                disposedValue = true;
            }
        }
        public Task MarkEventAsFailedAsync(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventStateEnum.PublishedFailed);
        }

        public Task MarkEventAsInProgressAsync(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventStateEnum.InProgress);
        }

        public Task MarkEventAsPublishedAsynn(Guid eventId)
        {
            return UpdateEventStatus(eventId, EventStateEnum.Published);
        }

        private Task UpdateEventStatus(Guid eventId,EventStateEnum status)
        {
            var eventLogEntry = _integrationEventLogContext.integrationEventLogs.Single(ie => ie.EventId == eventId);
            eventLogEntry.State = status;
            if (status == EventStateEnum.InProgress)
                eventLogEntry.TimesSent++;
            _integrationEventLogContext.integrationEventLogs.Update(eventLogEntry);
            return _integrationEventLogContext.SaveChangesAsync();
        }
        public async Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
        {
            var tid = transactionId.ToString();
            var result = await _integrationEventLogContext.integrationEventLogs.Where(e => e.TransactionId.Equals(tid) && e.State == EventStateEnum.NoPublished).ToListAsync();
            if(result!=null && result.Any())
            {
                return result.OrderBy(o => o.CreationTime).Select(e => e.DeserializeJsonContent(_eventtypes.Find(t => t.Name == e.EventTypeShortName)));
            }
            return new List<IntegrationEventLogEntry>();
        }

        public async Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction)
        {
            if(transaction==null) throw new ArgumentNullException(nameof(transaction));
            var eventLogEntry = new IntegrationEventLogEntry(@event, transaction.TransactionId);
            await _integrationEventLogContext.Database.UseTransactionAsync(transaction.GetDbTransaction());
            await _integrationEventLogContext.SaveChangesAsync();
        }
    }
}
