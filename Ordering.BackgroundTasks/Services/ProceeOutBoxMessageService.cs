using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.SeedWork;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Idempotency;

namespace Ordering.BackgroundTasks.Services
{
    public class ProceeOutBoxMessageService : BackgroundService
    {
        private readonly OrderingContext _orderingContext;
        private readonly IPublisher _publisher;
        public ProceeOutBoxMessageService(OrderingContext orderingContext,IPublisher publisher)
        {
            _orderingContext= orderingContext;
            _publisher= publisher;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var messages =await _orderingContext.Set<OutBoxMessage>().Where(m=>m.ProceddedOnUtc==null)
                .Take(10).ToListAsync(stoppingToken);
            foreach (var message in messages)
            {
                IDomainEvent? domainevent = System.Text.Json.JsonSerializer.Deserialize<IDomainEvent>(message.Content);
                if(domainevent is null)
                    continue;
               await _publisher.Publish(domainevent,stoppingToken);
                message.ProceddedOnUtc= DateTime.UtcNow;
            }
            await _orderingContext.SaveChangesAsync(stoppingToken);
        }
    }
}
