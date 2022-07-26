using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntegrationEventLogEF
{
    public class IntegrationEventLogEntry
    {
        private IntegrationEventLogEntry() { }

        public IntegrationEventLogEntry(IntegrationEvent @event,Guid transactionId)
        {
            EventId = @event.Id;
            CreationTime = @event.CreationDate;
            EventTypeName = @event.GetType().FullName ?? throw new ArgumentNullException(nameof(@event));
            Content = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });
            State = EventStateEnum.NoPublished;
            TransactionId = transactionId.ToString();
        }

        public Guid EventId { get; private set; }
        public string EventTypeName { get; private set; }
        public string EventTypeShortName=> EventTypeName.Split('.')?.Last()?? throw new ArgumentNullException(nameof(EventTypeName));
        public IntegrationEvent IntegrationEvent { get; private set; }
        public EventStateEnum State { get; set; }
        public int TimesSent { get; set; }
        public DateTime CreationTime { get; private set; }
        public string Content { get; private set; }
        public string TransactionId { get; private set; }
        public IntegrationEventLogEntry DeserializeJsonContent(Type type)
        {
            IntegrationEvent = JsonSerializer.Deserialize(Content,type,new JsonSerializerOptions { PropertyNameCaseInsensitive = true}) as IntegrationEvent ?? throw new Exception("类型转换失败") ;
            return this;
        }
    }
}
