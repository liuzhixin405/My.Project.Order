using EventBus.Extensions;
using Google.Protobuf.Collections;
using Grpc.Core;
using MediatR;

namespace GrpcOrdering
{
    public class OrderingService : OrderingGrpc.OrderingGrpcBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<OrderingService> _logger;
        public OrderingService(IMediator mediator, ILogger<OrderingService> logger)
        {
            this.mediator = mediator;
            _logger = logger;
        }
        public override async Task<OrderDraftDTO> CeateOrderDraftFromBasketData(CreateOrderDraftCommand createOrderDraftCommand, ServerCallContext context)
        {
            _logger.LogInformation("Begin grpc call from method {Method} for ordering get order draft {CreateOrderDraftCommand}", context.Method, createOrderDraftCommand);
            _logger.LogTrace(
                "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                createOrderDraftCommand.GetGenericTypeName(),
                nameof(createOrderDraftCommand.BuyerId),
                createOrderDraftCommand.BuyerId,
                createOrderDraftCommand);
            var command = new Ordering.API.Application.Commands.CreateOrderDraftCommand(createOrderDraftCommand.BuyerId, this.MapBasketItems(createOrderDraftCommand.Items));
            var data = await mediator.Send(command);
            if (data != null)
            {
                context.Status = new Status(StatusCode.OK, $"ordering get order draft {createOrderDraftCommand} do exist");

                return this.MapResponse(data);
            }
            else
            {
                context.Status = new Status(StatusCode.NotFound, $" ordering get order draft {createOrderDraftCommand} do not exist");
            }
            return new OrderDraftDTO();
        }

        public OrderDraftDTO MapResponse(Ordering.API.Application.Commands.OrderDraftDTO order)
        {
            var result = new OrderDraftDTO()
            {
                Total = (double)order.Total
            };
            order.OrderItems.ToList().ForEach(i => result.OrderItems.Add(new OrderItemDTO
            {
                Discount = (double)i.Discount,
                PictureUrl = i.PictureUrl,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = (double)i.UnitPrice,
                Units = i.Units,
            }));
            return result;
        }

        public IEnumerable<Ordering.API.Application.Models.BasketItem> MapBasketItems(RepeatedField<BasketItem> items)
        {
            return items.Select(x => new Ordering.API.Application.Models.BasketItem()
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                UnitPrice = (decimal)x.UnitPrice,
                OldUnitPrice = (decimal)x.OldUnitPrice,
                Quantity = x.Quantity,
                PictureUrl = x.PictureUrl,
            });
        }
    }
}
