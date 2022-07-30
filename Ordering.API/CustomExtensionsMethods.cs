﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using EventBus;
using EventBus.Abstractions;
using EventBusRabbitMQ;
using GrpcOrdering;
using HealthChecks.UI.Client;
using IntegrationEventLogEF;
using IntegrationEventLogEF.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ordering.API.Application.IntegrationEvents;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.API.Controllers;
using Ordering.API.Infrastructure.AutofacModules;
using Ordering.API.Infrastructure.Filters;
using Ordering.API.Infrastructure.Services;
using Ordering.Infrastructure;
using Polly;
using RabbitMQ.Client;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;

namespace Ordering.API
{
    //public class Startup
    //{
    //    public IConfiguration Configuration { get;}
    //    public Startup(IConfiguration configuration)
    //    {
    //        Configuration = configuration;
    //    }

    //    public virtual IServiceProvider ConfigureServices(IServiceCollection services)
    //    {
    //        services.AddGrpc(o =>
    //        {
    //            o.EnableDetailedErrors = true;
    //        })
    //            .Services
    //            .AddApplicationInsights(Configuration)
    //            .AddCustomMvc()
    //            .AddHealthChecks(Configuration)
    //            .AddCustomDbContext(Configuration)
    //            .AddCustomSwagger(Configuration)
    //            .AddCustomIntegrations(Configuration)
    //            .AddCustomConfiguration(Configuration)
    //            .AddEventBus(Configuration)
    //            .AddCustomAuthentication(Configuration);

    //        var container = new ContainerBuilder();
    //        container.Populate(services);
    //        container.RegisterModule(new MediatorModule());
    //        container.RegisterModule(new ApplicationModule(Configuration["ConnectionString"]));

    //        return new AutofacServiceProvider(container.Build());
    //    }

    //    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    //    {
    //        //loggerFactory.AddAzureWebAppDiagnostics();
    //        //loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

    //        var pathBase = Configuration["PATH_BASE"];
    //        if (!string.IsNullOrEmpty(pathBase))
    //        {
    //            app.UsePathBase(pathBase);
    //        }

    //        app.UseSwagger()
    //           .UseSwaggerUI(c =>
    //           {
    //               c.SwaggerEndpoint($"{(!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty)}/swagger/v1/swagger.json", "Ordering.API V1");
    //               c.OAuthClientId("orderingswaggerui");
    //               c.OAuthAppName("Ordering Swagger UI");
    //           });

    //        app.UseRouting();
    //        app.UseCors("CorsPolicy");
    //        ConfigureAuth(app);

    //        app.UseEndpoints(endpoints =>
    //        {
    //            endpoints.MapGrpcService<OrderingService>();
    //            endpoints.MapDefaultControllerRoute();
    //            endpoints.MapControllers();
    //            endpoints.MapGet("/_proto/", async ctx =>
    //            {
    //                ctx.Response.ContentType = "text/plain";
    //                using var fs = new FileStream(Path.Combine(env.ContentRootPath, "Proto", "basket.proto"), FileMode.Open, FileAccess.Read);
    //                using var sr = new StreamReader(fs);
    //                while (!sr.EndOfStream)
    //                {
    //                    var line = await sr.ReadLineAsync();
    //                    if (line != "/* >>" || line != "<< */")
    //                    {
    //                        await ctx.Response.WriteAsync(line);
    //                    }
    //                }
    //            });
    //            endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
    //            {
    //                Predicate = _ => true,
    //                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    //            });
    //            endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
    //            {
    //                Predicate = r => r.Name.Contains("self")
    //            });
    //        });

    //        ConfigureEventBus(app);
    //    }

    //    private void ConfigureEventBus(IApplicationBuilder app)
    //    {
    //        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

    //        eventBus.Subscribe<UserCheckoutAcceptedIntegrationEvent, IIntegrationEventHandler<UserCheckoutAcceptedIntegrationEvent>>();
    //        eventBus.Subscribe<GracePeriodConfirmedIntegrationEvent, IIntegrationEventHandler<GracePeriodConfirmedIntegrationEvent>>();
    //        eventBus.Subscribe<OrderStockConfirmedIntegrationEvent, IIntegrationEventHandler<OrderStockConfirmedIntegrationEvent>>();
    //        eventBus.Subscribe<OrderStockRejectedIntegrationEvent, IIntegrationEventHandler<OrderStockRejectedIntegrationEvent>>();
    //        eventBus.Subscribe<OrderPaymentFailedIntegrationEvent, IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>>();
    //        eventBus.Subscribe<OrderPaymentSucceededIntegrationEvent, IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>>();
    //    }

    //    protected virtual void ConfigureAuth(IApplicationBuilder app)
    //    {
    //        app.UseAuthentication();
    //        app.UseAuthorization();
    //    }
    //}

    static partial class CustomExtensionsMethods
    {
        public static IServiceCollection AddApplicationInsights(this IServiceCollection services,IConfiguration configuration)
        {

            return services.AddApplicationInsightsTelemetry(configuration);
        }
        public static IServiceCollection AddCustomMvc(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
            })
             // Added for functional tests
             .AddApplicationPart(typeof(OrdersController).Assembly)
             .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .SetIsOriginAllowed((host) => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });
            return services;
        }
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            hcBuilder
                .AddSqlServer(
                    configuration["ConnectionString"],
                    name: "OrderingDB-check",
                    tags: new string[] { "orderingdb" });

          
                hcBuilder
                    .AddRabbitMQ(
                        $"amqp://{configuration["EventBusConnection"]}",
                        name: "ordering-rabbitmqbus-check",
                        tags: new string[] { "rabbitmqbus" });
            

            return services;
        }

        public static IServiceCollection AddCustomDbContext(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddDbContext<OrderingContext>(os =>
            {
                os.UseSqlServer(configuration["ConnectionString"],
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                        sqlOptions.EnableRetryOnFailure(15, TimeSpan.FromSeconds(30), null);
                    });
            },
                    ServiceLifetime.Scoped
                );
            services.AddDbContext<IntegrationEventLogContext>(options =>
            {
                options.UseSqlServer(configuration["ConnectionString"],
                                     sqlServerOptionsAction: sqlOptions =>
                                     {
                                         sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                                         
                                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                                     });
            });

            return services;
        }
        
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Ordering HTTP API",
                    Version = "v1",
                    Description = "The Ordering Service HTTP API"
                });
                options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Type=Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
                    Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
                    {
                        Implicit = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize"),
                            Scopes = new Dictionary<string, string> { { "orders","Ordering API"} }
                        }
                    }
                });
                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });
            return services;
        }

        public static IServiceCollection AddCustomIntegrations(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IIdentityService, IdentityService>();
            services.AddTransient<Func<DbConnection, IIntegrationEventLogService>>(
                sp => (DbConnection c) => new IntegrationEventLogService(c));

            services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

                services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();


                    var factory = new ConnectionFactory()
                    {
                        HostName = configuration["EventBusConnection"],
                        DispatchConsumersAsync = true
                    };

                    if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                    {
                        factory.UserName = configuration["EventBusUserName"];
                    }

                    if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                    {
                        factory.Password = configuration["EventBusPassword"];
                    }

                    var retryCount = 5;
                    if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                    {
                        retryCount = int.Parse(configuration["EventBusRetryCount"]);
                    }

                    return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
                });
            
            return services;
        }
        public static IServiceCollection AddCustomConfiguration(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<OrderingSettings>(configuration);
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Please refer to the errors property for additional details."
                    };
                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json", "application/problem+xml" }
                    };
                };
            });
            return services;
        }

        public static IServiceCollection AddEventBus(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddSingleton<IEventBus, EventBusRabbitMQ.EventBusRMQ>(sp =>
            {
                var subscriptionClientName = configuration["SubscriptionClientName"];
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ.EventBusRMQ>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }
                return new EventBusRabbitMQ.EventBusRMQ(rabbitMQPersistentConnection, logger, iLifetimeScope, eventBusSubscriptionsManager, subscriptionClientName, retryCount);
            });
            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services,IConfiguration configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

            var identityUrl = configuration.GetValue<string>("IdentityUrl");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.Audience = "orders";
            });

            return services;
        }
    }
}