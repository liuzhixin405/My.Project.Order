using MediatR;
using Ordering.API.Application.Models;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using System.Linq;
namespace Ordering.API.Application.Commands
{
    public class CreateOrderDraftCommand:IRequest<OrderDraftDTO>
    {
        public string BuyerId { get; private set; }
        public IEnumerable<BasketItem> Items { get; private set; }
        public CreateOrderDraftCommand(string buyerId, IEnumerable<BasketItem> items)
        {
            BuyerId = buyerId;
            Items = items;
        }
    }

    public record OrderDraftDTO
    {
        public IEnumerable<OrderItemDTO> OrderItems { get; init; }
        public decimal Total { get; init; }

        public static OrderDraftDTO FromOrder(Order order)
        {
            return new OrderDraftDTO()
            {
                OrderItems = order.OrderItems.Select(item => new OrderItemDTO
                {
                    Discount = item.GetCurrentDiscount(),
                    ProductId = item.ProductId,
                    UnitPrice = item.GetUnitPrice(),
                    PictureUrl = item.GetPictureUri(),
                    Units = item.GetUnits(),
                    ProductName = item.GetOrderItemProductName()
                }),
                Total = order.GetTotal()
            };
        }
    }
}
