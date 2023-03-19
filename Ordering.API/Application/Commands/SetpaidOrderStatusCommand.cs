using MediatR;
using System.Runtime.Serialization;

namespace Ordering.API.Application.Commands
{
    public class SetpaidOrderStatusCommand:IRequest<bool>
    {
        [DataMember]
        public int OrderNumber { get; private set; }
        public SetpaidOrderStatusCommand(int orderNumber)
        {
            OrderNumber = orderNumber;
        }
    }
}
