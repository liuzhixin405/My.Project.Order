using Ordering.Domain.Exceptions;
using Ordering.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.AggregatesModel.BuyerAggregate
{
    public class PaymentMethod:Entity
    {
        private string _alias;
        private string _cardNumber;
        private string _securityNumber;
        private string _cardHolderName;
        private DateTime _expiration;

        private int _cardTypeId;
        public CardType CardType { get; private set; }

        protected PaymentMethod() { }
        public PaymentMethod(int cardTypeId,string alias, string cardNumber, string securityNumber, string cardHolderName, DateTime expiration)
        {
            _alias = alias;
            _cardNumber = cardNumber;
            _securityNumber = securityNumber;
            _cardHolderName = cardHolderName;
            _expiration = expiration;
            _cardTypeId = cardTypeId;
            if (expiration < DateTime.UtcNow)
                throw new OrderingDomainException(nameof(expiration));
        }
        public bool IsEqualTo(int cardTypeId,string cardNumber,DateTime expiration)
        {
            return _cardTypeId == cardTypeId && _cardNumber == cardNumber && _expiration == expiration;
        }
    }
}
