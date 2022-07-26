﻿using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.Events
{
    public class OrderStatusChangedToWaitingValidationDoomainEvent:INotification
    {
        public int OrderId { get; }
        public IEnumerable<OrderItem> OrderItems { get; }
        public OrderStatusChangedToWaitingValidationDoomainEvent(int orderId,IEnumerable<OrderItem> orderItems)
        {
            OrderId = orderId;
            OrderItems = orderItems;
        }
    }
}
