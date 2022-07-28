using MediatR;
using Ordering.API.Extensions;
using Ordering.API.Infrastructure.Services;
using Ordering.Domain.AggregatesModel.OrderAggregate;

namespace Ordering.API.Application.Commands
{
    public class CreateOrderDraftCommandHandler : IRequestHandler<CreateOrderDraftCommand, OrderDraftDTO>
    {

        public Task<OrderDraftDTO> Handle(CreateOrderDraftCommand request, CancellationToken cancellationToken)
        {
            var order = Order.NewDraft();
            var orderItems = request.Items.Select(i => i.ToOrderItemDTO());
            foreach (var item in orderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }
            return Task.FromResult(OrderDraftDTO.FromOrder(order));
        }
    }
}
