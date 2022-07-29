using Autofac;
using EventBus.Abstractions;
using Ordering.API.Application.Commands;
using Ordering.API.Application.Queries;
using Ordering.Domain.AggregatesModel.BuyerAggregate;
using Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.Infrastructure.Idempotency;
using Ordering.Infrastructure.Repositories;
using System.Reflection;

namespace Ordering.API.Infrastructure.AutofacModules
{
    public class ApplicationModule:Autofac.Module
    {
        public string QueriesConnectionString { get; }
        public ApplicationModule(string queriesConnectionString)
        {
            QueriesConnectionString = queriesConnectionString;
        }
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c=>new OrderQueries(QueriesConnectionString))
                .As<IOrderQueries>().InstancePerLifetimeScope();
            builder.RegisterType<BuyerRepository>()
                .As<IBuyerRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<OrderRepository>()
                .As<IOrderRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<RequestManager>()
            .As<IRequestManager>()
            .InstancePerLifetimeScope();
            builder.RegisterAssemblyTypes(typeof(CreateOrderCommandHandler).GetTypeInfo().Assembly)
               .AsClosedTypesOf(typeof(IIntegrationEventHandler<>));
        }
    }
}
