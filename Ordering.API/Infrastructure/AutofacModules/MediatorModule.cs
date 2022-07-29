using Autofac;
using FluentValidation;
using MediatR;
using Ordering.API.Application.Behaviors;
using Ordering.API.Application.Commands;
using Ordering.API.Application.DomainEventHandlers.OrderStartedEvent;
using Ordering.API.Application.Validations;
using System.Reflection;

namespace Ordering.API.Infrastructure.AutofacModules
{
    public class MediatorModule:Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly).AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(typeof(CreateOrderCommand).GetTypeInfo().Assembly).AsClosedTypesOf(typeof(IRequestHandler<,>));
            builder.RegisterAssemblyTypes(typeof(ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler).GetTypeInfo().Assembly)
               .AsClosedTypesOf(typeof(INotificationHandler<>));
            builder
           .RegisterAssemblyTypes(typeof(CreateOrderCommandValidator).GetTypeInfo().Assembly)
           .Where(t => t.IsClosedTypeOf(typeof(IValidator<>)))
           .AsImplementedInterfaces();

            builder.Register<ServiceFactory>(context =>
            {
                var componentContext = context.Resolve<IComponentContext>();
                return t => { object o; return componentContext.TryResolve(t, out o) ? o : null; };
            });
            builder.RegisterGeneric(typeof(LoggingBehavior<,>)).As(typeof(IPipelineBehavior<,>));
            builder.RegisterGeneric(typeof(ValidatorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
            builder.RegisterGeneric(typeof(TransactionBehaviour<,>)).As(typeof(IPipelineBehavior<,>));
        }
    }
}
