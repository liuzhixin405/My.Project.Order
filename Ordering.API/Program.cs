using Autofac.Extensions.DependencyInjection;
using Azure.Core;
using IntegrationEventLogEF;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ordering.API;
using Ordering.API.Infrastructure;
using Ordering.Infrastructure;
using Polly;
using Serilog;
using System.Net;

var configuration = GetConfiguration();
Log.Logger = CreateSerilogLogger(configuration);

try
{
    Log.Information("Configuring web host ({ApplicationContext})...", Program.AppName);
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.CaptureStartupErrors(false).ConfigureKestrel(options =>
    {
        var ports = GetDefinedPorts(configuration);
        options.Listen(IPAddress.Any, ports.httpPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });

        options.Listen(IPAddress.Any, ports.grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }).ConfigureAppConfiguration(x => x.AddConfiguration(configuration));
    builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory());
    builder.Host.UseSerilog();
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
   
    //builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    //builder.Services.AddEndpointsApiExplorer();
    //builder.Services.AddSwaggerGen();
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);
   
    Log.Information("Starting web host ({ApplicationContext})...", Program.AppName);
    var app = builder.Build();
    startup.Configure(app, builder.Environment);
   
    var service = builder.Services.BuildServiceProvider() ?? throw new ArgumentNullException(nameof(builder.Services));
    MigrateDbContext<OrderingContext>(service, (context, services) =>
    {
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var settings = services.GetService<IOptions<OrderingSettings>>();
        var logger = services.GetService<ILogger<OrderingContextSeed>>();
        new OrderingContextSeed().SeedAsync(context, env, settings, logger).Wait();
    });
    MigrateDbContext<IntegrationEventLogContext>(service, (_, _) => { });
    
    app.Run();

}
catch(Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", Program.AppName);
}
finally
{
    Log.CloseAndFlush();
}
(int httpPort,int grpcPort) GetDefinedPorts(IConfiguration config)
{
    var grpcPort = config.GetValue("GRPC_PORT", 5001);
    var port = config.GetValue("PORT", 80);
    return (port, grpcPort);
}

// Add services to the container.




IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", false, true)
        .AddEnvironmentVariables();
    return builder.Build();
}
Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
{
    var seqServerUrl = configuration["Serilog:SeqServerUrl"];
    var logstashUrl = configuration["Serilog:LogstashgUrl"];
    return new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.WithProperty("ApplicationContext", Program.AppName)
        .Enrich.FromLogContext()
        .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
        .WriteTo.Seq(string.IsNullOrWhiteSpace(seqServerUrl) ? "http://seq" : seqServerUrl)
        .WriteTo.Http(string.IsNullOrWhiteSpace(logstashUrl) ? "http://logstash:8080" : logstashUrl, null, null, null, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

void MigrateDbContext<TContext>(IServiceProvider service,Action<TContext,IServiceProvider> seeder) where TContext : DbContext
{
    using(var scope = service.CreateAsyncScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var context = services.GetService<TContext>();
        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);
            var retries = 10;
            var retry = Policy.Handle<SqlException>()
                .WaitAndRetry(
                retries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retry, ctx) => {
                    logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retries}", nameof(TContext), exception.GetType().Name, exception.Message, retry, retries);
                });
            retry.Execute(() => InvokeSeeder(seeder,context,services));
        }catch(Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
        }
    }
}

void InvokeSeeder<TContext>(Action<TContext,IServiceProvider> seeder,TContext context,IServiceProvider services)
    where TContext : DbContext
{
    context.Database.Migrate();
    seeder(context, services);
}















public partial class Program
{
    public static string Namespace = typeof(OrderingSettings).Namespace;
    public static string AppName = Namespace.Substring(Namespace.LastIndexOf(".", Namespace.LastIndexOf(".") - 1) + 1);
}

