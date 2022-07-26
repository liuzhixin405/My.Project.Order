using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EventBus.Events
{
    public record IntegrationEvent
    {
        [JsonConstructor]
        public IntegrationEvent(Guid id,DateTime createDate)
        {
            Id = id;
            CreationDate = createDate;
        }

        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }
        [JsonInclude]
        public Guid Id { get; set; }
        [JsonInclude]
        public DateTime CreationDate { get; private init; }
        
    }
}
