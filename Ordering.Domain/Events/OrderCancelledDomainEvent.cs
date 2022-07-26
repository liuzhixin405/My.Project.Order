using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.Events
{
    public class OrderCancelledDomainEvent:INotification
    {
        public Order Order { get; }
        public OrderCancelledDomainEvent(Order  order)
        {
            Order = order;
        }
    }
}
