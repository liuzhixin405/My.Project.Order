using Ordering.Domain.Exceptions;
using Ordering.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.AggregatesModel.OrderAggregate
{
    public class OrderItem:Entity
    {
        private string _productName;
        private string _pictureUrl;
        private decimal _unitPrice;
        private decimal _discount;
        private int _units;
        public int ProductId { get; private set; }

        protected OrderItem() { }
        public OrderItem(int productId,string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
        {
            if (units <= 0)
                throw new OrderingDomainException("Invalid number of units");
            if(units *unitPrice < discount)
                throw new OrderingDomainException("The total of order item is lower than applied discount");
            ProductId = productId;
            _productName = productName;
            _unitPrice = unitPrice;
            _discount = discount;
            _units = units;
            _pictureUrl = pictureUrl;
        }

        public string GetPictureUri() => _pictureUrl;

        public decimal GetCurrentDiscount()
        {
            return _discount;
        }
        public int GetUnits()
        {
            return _units;
        }

        public decimal GetUnitPrice()
        {
            return _unitPrice;
        }
        public string GetOrderItemProductName() => _productName;
        public void SetNewDiscount(decimal discount)
        {
            if(discount < 0)
                throw new OrderingDomainException("Discount is not valid");
            _discount = discount;
        }
        public void AddUnits(int units)
        {
            if (units < 0)
                throw new OrderingDomainException("Invalid units");
            _units += units;
        }
    }
}
