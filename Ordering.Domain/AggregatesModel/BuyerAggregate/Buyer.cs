using Ordering.Domain.Events;
using Ordering.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Domain.AggregatesModel.BuyerAggregate
{
    public class Buyer:Entity,IAggregateRoot
    {
        public string IdentityGuid { get; private set; }
        public string Name { get; private set; }
        private List<PaymentMethod> _paymentMethods;
        public IEnumerable<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

        protected Buyer()
        {
            _paymentMethods = new List<PaymentMethod>();
        }
        public Buyer(string identity, string name, List<PaymentMethod> paymentMethods):this()
        {
            IdentityGuid = identity??throw new ArgumentNullException(nameof(identity));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
        public PaymentMethod VerifyOrAddPaymentMethod(int cardTypeId,string alias,string cardNumber,string securityNumber,string cardHolderName,DateTime expiration,int orderId)
        {
            var payment = _paymentMethods.SingleOrDefault(p => p.IsEqualTo(cardTypeId, cardNumber, expiration));
            
            if (payment == null)
            {
                payment = new PaymentMethod(cardTypeId, alias, cardNumber, securityNumber, cardHolderName, expiration);
                _paymentMethods.Add(payment);
            }
            AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, payment, orderId));
            return payment;

        }
    }
}
