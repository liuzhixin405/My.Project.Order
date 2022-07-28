using MediatR;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.API.Application.Commands
{
    public class CancelOrderCommandHandler:IRequestHandler<CancelOrderCommand,bool>
    {
        private readonly IOrderRepository _orderRepository;
        public CancelOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var orderToUpdate = await _orderRepository.GetAsync(request.OrderNumber);
            if (orderToUpdate == null)
                return false;
            orderToUpdate.SetCancelledStatus();
            return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
    }

    public class CancelOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CancelOrderCommand, bool>
    {
        public CancelOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager, ILogger<IdentifiedCommandHandler<CancelOrderCommand, bool>> logger) :base(mediator,requestManager,logger)
        {

        }
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}
