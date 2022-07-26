using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEventLogEF.Utilities
{
    public class ResilientTransaction
    {
        private DbContext _context;
        private ResilientTransaction(DbContext context)=>_context=context?? throw new ArgumentNullException(nameof(context));

        public static ResilientTransaction New(DbContext context) => new ResilientTransaction(context);

        public async Task ExecuteAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using(var transaction =await _context.Database.BeginTransactionAsync())
                {
                    await action();
                    await transaction.CommitAsync();
                }
            });
        }
    
    }
}
