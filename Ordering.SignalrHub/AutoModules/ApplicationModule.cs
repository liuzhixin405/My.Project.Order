using Autofac;
using EventBus.Abstractions;
using Ordering.SignalrHub.IntegrationEvents;
using System.Reflection;

namespace Ordering.SignalrHub.AutoModules
{
    public class ApplicationModule:Autofac.Module
    {
        public string QueriesConnectionString { get; }

        public ApplicationModule()
        {

        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(OrderStatusChangedToAwaitingValidationIntegrationEvent).GetTypeInfo().Assembly)
                .AsClosedTypesOf(typeof(IIntegrationEventHandler<>));
        }
    }
}
