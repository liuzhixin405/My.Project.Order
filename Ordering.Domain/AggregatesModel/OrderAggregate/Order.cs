using Ordering.Domain.Events;
using Ordering.Domain.Exceptions;
using Ordering.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.AggregatesModel.OrderAggregate
{
    public class Order:Entity,IAggregateRoot
    {
        private DateTime _orderDate;
        public Address Address { get; private set; }
        public int? GetBuyerId => _buyerId;
        private int? _buyerId;

        public OrderStatus OrderStatus { get; private set; }
        private int _orderStatusId;
        private string _description;

        private bool _isDraft;

        private readonly List<OrderItem> _orderItems;

        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems;
        private int? _paymentMethodId;

        public static Order NewDraft()
        {
            var order = new Order();
            order._isDraft = true;
            return order;
        }
        protected Order()
        {
            _orderItems = new List<OrderItem>();
            _isDraft = false;
        }
        public Order(string userId,string userName,Address address,int cardTypeId,string cardNumber,string cardSecurityNumber,string cardHolderName,DateTime cardExpiration,int? buyerId=null,int? paymentMethodId=null):this()
        {
            _buyerId = buyerId;
            _paymentMethodId = paymentMethodId;
            _orderStatusId = OrderStatus.Submitted.Id;
            _orderDate = DateTime.UtcNow;
            Address = address;
            AddOrderStartedDomainEvent(userId, userName, cardTypeId, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration);
        }

        public void AddOrderItem(int productId,string productName,decimal unitPrice,decimal discount,string pictureUrl,int units = 1)
        {
            var existingOrderForProduct = _orderItems.Where(o => o.ProductId == productId).SingleOrDefault();
            if (existingOrderForProduct !=null)
            {
                if(discount > existingOrderForProduct.GetCurrentDiscount())
                {
                    existingOrderForProduct.SetNewDiscount(discount);
                }
                existingOrderForProduct.AddUnits(units);
            }
            else
            {
                var orderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);
                _orderItems.Add(orderItem);
            }
        }
        public void SetPaymentId(int id)
        {
            _paymentMethodId = id;
        }

        public void SetBuyerId(int id)
        {
            _buyerId = id;
        }
        public void SetAwaitingValidationStatus()
        {
            if(_orderStatusId == OrderStatus.Submitted.Id)
            {
                AddDomainEvent(new OrderStatusChangedToWaitingValidationDoomainEvent(Id, _orderItems));
                _orderStatusId = OrderStatus.AwaitingValidation.Id;
            }
        }

        public void SetStockConfirmedStatus()
        {
            if(_orderStatusId == OrderStatus.AwaitingValidation.Id)
            {
                AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));
                _orderStatusId = OrderStatus.StockedConfirmed.Id;
                _description = "All the times were confirmed with avalibale stock.";
            }
        }
        public void SetPaidStatus()
        {
            if(_orderStatusId == OrderStatus.StockedConfirmed.Id)
            {
                AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, _orderItems));

                _orderStatusId = OrderStatus.Paid.Id;
                _description = "The payment was performed at a simulated \"American Bank checking bank account ending on XX35071\"";
            }
        }
        public void SetShippedStatus()
        {
            if(_orderStatusId != OrderStatus.Shipped.Id)
            {
                StatusChangeException(OrderStatus.Shipped);
            }
            _orderStatusId = OrderStatus.Shipped.Id;
            _description = "The Order was shipped.";
            AddDomainEvent(new OrderShippedDomainEvent(this));
        }

        public void SetCancelledStatus()
        {
            if (_orderStatusId == OrderStatus.Paid.Id || _orderStatusId == OrderStatus.Shipped.Id)
                StatusChangeException(OrderStatus.Cancelled);
            _orderStatusId = OrderStatus.Cancelled.Id;
            _description = "The Order was cancelled.";
            AddDomainEvent(new OrderCancelledDomainEvent(this));
        }
        public void SetCancelledStatusWhenStockIsRegected(IEnumerable<int> orderStockRegectedItems)
        {
            if (_orderStatusId == OrderStatus.AwaitingValidation.Id)
            {
                _orderStatusId = OrderStatus.Cancelled.Id;
                var itemsStockRejectedDescription = OrderItems.Where(c => orderStockRegectedItems.Contains(c.ProductId))
                    .Select(c => c.GetOrderItemProductName());
                var itemStockRejectedDescription = string.Join(",", itemsStockRejectedDescription);
                _description = $"The product items don't have stock: ({itemsStockRejectedDescription}).";
            }
        }
        private void AddOrderStartedDomainEvent(String userId,string userName,int cardTypeId,string cardNumber,string cardSecurityNumber,string cardHolderName,DateTime cardExpiration)
        {
            var orderStartedDomainEvent = new OrderStartedDomainEvent(this, userId, userName, cardTypeId,
                                                                      cardNumber, cardSecurityNumber,
                                                                      cardHolderName, cardExpiration);
            this.AddDomainEvent(orderStartedDomainEvent);
        }
        private void StatusChangeException(OrderStatus orderStatusToChange)
        {
            throw new OrderingDomainException($"Is not possible to change the order status from {OrderStatus.Name} to {orderStatusToChange.Name}.");
        }

        public decimal GetTotal()
        {
            return _orderItems.Sum(o => o.GetUnits() * o.GetUnitPrice());
        }
    }
}
