using MediatR;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.API.Infrastructure.Services;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.API.Application.Commands
{
    public class CreateOrderCommandHandler:IRequestHandler<CreateOrderCommand,bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IdentityService _identityService;
        private readonly IMediator _mediator;
        private IOrderingIntegrationEventService _orderingIntegrationEventService;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(IMediator mediator, IOrderingIntegrationEventService orderingIntegrationEventService, IOrderRepository orderRepository, IdentityService identityService, 
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _identityService = identityService;
            _mediator = mediator;
            _orderingIntegrationEventService = orderingIntegrationEventService;
            _logger = logger;
        }

        public async Task<bool> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(request.UserId);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

            var address = new Address(request.Street, request.City, request.State, request.Country, request.ZipCode);
            var order = new Order(request.UserId, request.UserName, address, request.CardTypeId, request.CardNumber,request.CardSecurityNumber, request.CardHolderName, request.CardExpiration);
            foreach (var item in request.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }
            _logger.LogInformation("----- Creating Order - Order: {@Order}", order);
            return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
    }

    public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
    {
        public CreateOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager, ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger) : base(mediator, requestManager, logger)
        {
        }
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}
