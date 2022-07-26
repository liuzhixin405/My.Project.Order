using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.Events
{
    public class OrderStatusChangedToPaidDomainEvent:INotification
    {
        public int OrderId { get; }
        public IEnumerable<OrderItem> OrderItems { get; }
        public OrderStatusChangedToPaidDomainEvent(int orderId, IEnumerable<OrderItem> orderItems)
        {
            OrderId = orderId;
            OrderItems = orderItems;
        }
    }
}
