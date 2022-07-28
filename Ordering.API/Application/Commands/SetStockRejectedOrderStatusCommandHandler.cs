using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.API.Application.Commands
{
    public class SetStockRejectedOrderStatusCommandHandler : IRequestHandler<SetStockRejectedOrderStatusCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        public SetStockRejectedOrderStatusCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<bool> Handle(SetStockRejectedOrderStatusCommand request, CancellationToken cancellationToken)
        {
            await Task.Delay(10000, cancellationToken);
            var orderToUpdate = await _orderRepository.GetAsync(request.OrderNumber);
            if (orderToUpdate == null) return false;
            orderToUpdate.SetCancelledStatusWhenStockIsRegected(request.OrderStockItems);
            return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
    }
    public class SetStockRegectedOrderStatusIdentifiedCommandHandler : IdentifiedCommandHandler<SetStockRejectedOrderStatusCommand, bool>
    {
        public SetStockRegectedOrderStatusIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager,
          ILogger<IdentifiedCommandHandler<SetStockRejectedOrderStatusCommand, bool>> logger) : base(mediator, requestManager, logger)
        {

        }
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}
