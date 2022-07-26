using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.Events
{
    public class OrderShippedDomainEvent:INotification
    {
        public Order Order { get; }
        public OrderShippedDomainEvent(Order order)
        {
            Order = order;
        }
    }
}
