using MediatR;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.API.Application.Commands
{
    public class SetAwaitingValidationIdentifiedOrderStatusCommandHandler:IdentifiedCommandHandler<SetAwaitingValidationOrderStatusCommand,bool>
    {
        public SetAwaitingValidationIdentifiedOrderStatusCommandHandler(IMediator mediator,IRequestManager requestManager,
            ILogger<IdentifiedCommandHandler<SetAwaitingValidationOrderStatusCommand,bool>> logger):base(mediator,requestManager,logger)
        {

        }
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}
