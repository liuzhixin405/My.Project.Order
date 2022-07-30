using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure.Core;
using EventBus.Abstractions;
using GrpcOrdering;
using HealthChecks.UI.Client;
using IntegrationEventLogEF;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ordering.API;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.API.Infrastructure;
using Ordering.API.Infrastructure.AutofacModules;
using Ordering.Infrastructure;
using Polly;
using Serilog;
using System.Net;

IConfiguration configuration = GetConfiguration();
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
 
    //builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory()).UseStartup<Startup>();
    builder.Host.UseSerilog();
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureServices(services =>
    {
        services.AddGrpc(o =>
        {
            o.EnableDetailedErrors = true;
        })
               .Services
               .AddApplicationInsights(configuration)
               .AddCustomMvc()
               .AddHealthChecks(configuration)
               .AddCustomDbContext(configuration)
               .AddCustomSwagger(configuration)
               .AddCustomIntegrations(configuration)
               .AddCustomConfiguration(configuration)
               .AddEventBus(configuration)
               .AddCustomAuthentication(configuration);

        var container = new ContainerBuilder();
        container.Populate(services);
        container.RegisterModule(new MediatorModule());
        container.RegisterModule(new ApplicationModule(configuration["ConnectionString"]));

        //return new AutofacServiceProvider(container.Build());
    });
    
    //var startup = new Startup(builder.Configuration);
    //startup.ConfigureServices(builder.Services);


    Log.Information("Starting web host ({ApplicationContext})...", Program.AppName);
    var app = builder.Build();
    MigrateDbContext<OrderingContext>(app.Services,async (context, services) =>
    {
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var settings = services.GetService<IOptions<OrderingSettings>>();
        var logger = services.GetService<ILogger<OrderingContextSeed>>();
        await new OrderingContextSeed().SeedAsync(context, env, settings, logger);
    });
    MigrateDbContext<IntegrationEventLogContext>(app.Services,(_, _) => { });
    //startup.Configure(app, builder.Environment);
    Configure(app, app.Environment);
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

void MigrateDbContext<TContext>(IServiceProvider service, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
{
    using (var scope = service.CreateAsyncScope())
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
                (exception, timeSpan, retry, ctx) =>
                {
                    logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retries}", nameof(TContext), exception.GetType().Name, exception.Message, retry, retries);
                });
            retry.Execute(() => InvokeSeeder<TContext>(seeder, context, services));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
        }
    }
}

void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, TContext context, IServiceProvider services)
    where TContext : DbContext
{
    context.Database.EnsureCreated();
    context.Database.Migrate();
    seeder(context, services);
}
void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    var pathBase = configuration["PATH_BASE"];
    if (!string.IsNullOrEmpty(pathBase))
    {
        app.UsePathBase(pathBase);
    }

    app.UseSwagger()
       .UseSwaggerUI(c =>
       {
           c.SwaggerEndpoint($"{(!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty)}/swagger/v1/swagger.json", "Ordering.API V1");
           c.OAuthClientId("orderingswaggerui");
           c.OAuthAppName("Ordering Swagger UI");
       });
    app.UseRouting();
    app.UseCors("CorsPolicy");
    ConfigureAuth(app);

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGrpcService<OrderingService>();
        endpoints.MapDefaultControllerRoute();
        endpoints.MapControllers();
        endpoints.MapGet("/_proto/", async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            using var fs = new FileStream(Path.Combine(env.ContentRootPath, "Proto", "basket.proto"), FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                var line = await sr.ReadLineAsync();
                if (line != "/* >>" || line != "<< */")
                {
                    await ctx.Response.WriteAsync(line);
                }
            }
        });
        endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
        {
            Predicate = r => r.Name.Contains("self")
        });
    });

    ConfigureEventBus(app);
}

void ConfigureEventBus(IApplicationBuilder app)
{
    var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

    eventBus.Subscribe<UserCheckoutAcceptedIntegrationEvent, IIntegrationEventHandler<UserCheckoutAcceptedIntegrationEvent>>();
    eventBus.Subscribe<GracePeriodConfirmedIntegrationEvent, IIntegrationEventHandler<GracePeriodConfirmedIntegrationEvent>>();
    eventBus.Subscribe<OrderStockConfirmedIntegrationEvent, IIntegrationEventHandler<OrderStockConfirmedIntegrationEvent>>();
    eventBus.Subscribe<OrderStockRejectedIntegrationEvent, IIntegrationEventHandler<OrderStockRejectedIntegrationEvent>>();
    eventBus.Subscribe<OrderPaymentFailedIntegrationEvent, IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>>();
    eventBus.Subscribe<OrderPaymentSucceededIntegrationEvent, IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>>();
}

void ConfigureAuth(IApplicationBuilder app)
{
    app.UseAuthentication();
    app.UseAuthorization();
}
static partial class CustomExtensionsMethods
{
    public static IWebHost MigrateDbContext<TContext>(this IWebHost host, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
    {
        using (var scope = host.Services.CreateScope())
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
                        retryCount: retries,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (exception, timeSpan, retry, ctx) =>
                        {
                            logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retries}", nameof(TContext), exception.GetType().Name, exception.Message, retry, retries);
                        });

                //if the sql server container is not created on run docker compose this
                //migration can't fail for network related exception. The retry options for DbContext only 
                //apply to transient exceptions
                // Note that this is NOT applied when running some orchestrators (let the orchestrator to recreate the failing service)
                retry.Execute(() => InvokeSeeder(seeder, context, services));


                logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

            }
        }

        return host;
    }
    private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, TContext context, IServiceProvider services)
      where TContext : DbContext
    {
        context.Database.Migrate();
        seeder(context, services);
    }
}
    public partial class Program
{
    public static string Namespace = typeof(OrderingSettings).Namespace;
    public static string AppName = Namespace.Substring(Namespace.LastIndexOf(".", Namespace.LastIndexOf(".") - 1) + 1);
}

