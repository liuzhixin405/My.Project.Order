using Autofac;
using Autofac.Core;
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

    //builder.WebHost.CaptureStartupErrors(false).ConfigureKestrel(options =>
    //{
    //    var ports = GetDefinedPorts(configuration);
    //    options.Listen(IPAddress.Any, ports.httpPort, listenOptions =>
    //    {
    //        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    //    });

    //    options.Listen(IPAddress.Any, ports.grpcPort, listenOptions =>
    //    {
    //        listenOptions.Protocols = HttpProtocols.Http2;
    //    });
    //}).ConfigureAppConfiguration(x => x.AddConfiguration(configuration));
 
    //builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory()).UseStartup<Startup>();
    builder.Host.UseSerilog();
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Services.AddGrpc(o => o.EnableDetailedErrors = true);
    builder.Services
               .AddApplicationInsights(configuration)
               .AddCustomMvc()
               .AddHealthChecks(configuration)
               .AddCustomDbContext(configuration)
               .AddCustomSwagger(configuration)
               .AddCustomIntegrations(configuration)
               .AddCustomConfiguration(configuration)
               .AddEventBus(configuration)
               .AddCustomAuthentication(configuration);
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new MediatorModule());
        builder.RegisterModule(new ApplicationModule(configuration["ConnectionString"]));
    });

    builder.MigrateDbContext<OrderingContext>(() =>{})
        .MigrateDbContext<IntegrationEventLogContext>(() => { });
    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var services = scope.ServiceProvider;
    {
        var context = services.GetService<OrderingContext>();
        var env = services.GetService<IWebHostEnvironment>();
        var settings = services.GetService<IOptions<OrderingSettings>>();
        var logger = services.GetService<ILogger<OrderingContextSeed>>();
        new OrderingContextSeed().SeedAsync(context, env, settings, logger).Wait();
    };


    Log.Information("Starting web host ({ApplicationContext})...", Program.AppName);
    var app = builder.Build();
   
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
    public static WebApplicationBuilder MigrateDbContext<TContext>(this WebApplicationBuilder builder,Action action) where TContext : DbContext
    {

        using (var services = builder.Services.BuildServiceProvider())
        {
            var scope = services.CreateAsyncScope();
            var logger = services.GetRequiredService<ILogger<TContext>>();
            var context = scope.ServiceProvider.GetService<TContext>();

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
                retry.Execute(() => InvokeSeeder(action, context));


                logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

            }
        }

        return builder;
    }
    private static void InvokeSeeder<TContext>(Action seeder, TContext context)
      where TContext : DbContext
    {
        if (!(context.GetService<IDatabaseCreator>() as RelationalDatabaseCreator).Exists()) //数据库不存在自动创建，并建表
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        //context.Database.Migrate();
        seeder();
    }
}
    public partial class Program
{
    public static string Namespace = typeof(OrderingSettings).Namespace;
    public static string AppName = Namespace.Substring(Namespace.LastIndexOf(".", Namespace.LastIndexOf(".") - 1) + 1);
}

