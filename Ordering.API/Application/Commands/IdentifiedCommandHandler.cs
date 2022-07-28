using EventBus.Extensions;
using MediatR;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.API.Application.Commands
{
    public class IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R> where T : IRequest<R>
    {
        private readonly IMediator _mediator;
        private readonly IRequestManager _requestManager;
        private readonly ILogger<IdentifiedCommandHandler<T, R>> _logger;
        public IdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager, ILogger<IdentifiedCommandHandler<T, R>> logger)
        {
            _mediator = mediator;
            _requestManager = requestManager;
            _logger = logger;
        }
        protected virtual R CreateResultForDuplicateRequest()
        {
            return default(R);
        }

        public async Task<R> Handle(IdentifiedCommand<T, R> request, CancellationToken cancellationToken)
        {
            var alreadyExists = await _requestManager.ExistAsync(request.Id);
            if (alreadyExists)
            {
                return CreateResultForDuplicateRequest();
            }
            else
            {
                await _requestManager.CreateRequestForCommandAsync<T>(request.Id);
                try
                {
                    var command = request.Command;
                    var commandName = command.GetGenericTypeName();
                    var idProperty = string.Empty;
                    var commandId = string.Empty;

                    switch (command)
                    {
                        case CreateOrderCommand createOrderCommand:
                            idProperty = nameof(createOrderCommand.UserId);
                            commandId = createOrderCommand.UserId;
                            break;
                        case CancelOrderCommand cancelOrderCommand:
                            idProperty = nameof(cancelOrderCommand.OrderNumber);
                            break;
                        case ShipOrderCommand shipOrderCommand:
                            idProperty = nameof(shipOrderCommand.OrderNumber);
                            break;
                        default:
                            idProperty = "Id?";
                            commandId = "n/a";
                            break;
                    }
                    _logger.LogInformation(
                         "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                        commandName,
                        idProperty,
                        commandId,
                        command);
                    var result = await _mediator.Send(command, cancellationToken);

                    _logger.LogInformation(
                       "----- Command result: {@Result} - {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                       result,
                       commandName,
                       idProperty,
                       commandId,
                       command);
                    return result;
                }
                catch
                {
                    return default(R);
                }
            }

        }
    }
}
