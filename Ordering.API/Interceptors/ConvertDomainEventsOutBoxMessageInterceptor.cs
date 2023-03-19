using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ordering.Domain.SeedWork;
using System;
using System.Data;
using System.Text.Json.Serialization;

namespace Ordering.API.Interceptors
{
    public sealed class ConvertDomainEventsOutBoxMessageInterceptor:SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            DbContext? dbContxt = eventData.Context;
            if(dbContxt is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
            var events = dbContxt.ChangeTracker.Entries<Entity>().Select(x => x.Entity).SelectMany(x =>
            {
                var domains = x.DomainEvents;
                x.ClearDomainEvents();
                return domains;

            }).Select(x => new OutBoxMessage {
            Id=Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            Type = x.GetType().Name,
            Content = System.Text.Json.JsonSerializer.Serialize(x)
            }).ToList();
             dbContxt.Set<OutBoxMessage>().AddRangeAsync(events);
           return base.SavingChangesAsync(eventData, result, cancellationToken); //后台定时任务获取事件，再发送，就不再需要再dbcontext里面用mediator
        }
    }
}
