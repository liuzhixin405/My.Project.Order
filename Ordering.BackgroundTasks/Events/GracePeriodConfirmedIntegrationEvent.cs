using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.BackgroundTasks.Events
{
    public record GracePeriodConfirmedIntegrationEvent:IntegrationEvent
    {
        public int OrderId { get; }
        public GracePeriodConfirmedIntegrationEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}
