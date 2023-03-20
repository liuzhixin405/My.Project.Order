using IntegrationEventLogEF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ordering.API.Infrastructure.IntegrationEventMigrations
{
    public class IntegrationEventLogContextDesignTimeFactory : IDesignTimeDbContextFactory<IntegrationEventLogContext>
    {
        //private readonly IConfiguration _configuration;
        //public IntegrationEventLogContextDesignTimeFactory(IConfiguration configuration)
        //{

        //    _configuration = configuration;

        //}//不支持
        public IntegrationEventLogContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IntegrationEventLogContext>();
            optionsBuilder.UseSqlServer("Server=IOS;Database=OrderingDb;User Id=sa;Password=1230;TrustServerCertificate=true", options => options.MigrationsAssembly(GetType().Assembly.GetName().Name));

            return new IntegrationEventLogContext(optionsBuilder.Options);
        }
    }
}
