﻿using EventBus.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Application.IntegrationEvents;
using Ordering.Infrastructure;

namespace Ordering.API.Application.Behaviors
{
    public class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TransactionBehaviour<TRequest,TResponse>> _logger;
        private readonly OrderingContext _dbContext;
        private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
        public TransactionBehaviour(ILogger<TransactionBehaviour<TRequest, TResponse>> logger, OrderingContext dbContext, IOrderingIntegrationEventService orderingIntegrationEventService)
        {
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
            _dbContext = dbContext;
            _orderingIntegrationEventService = orderingIntegrationEventService;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();
            try
            {
                if (_dbContext.HasActiveTransaction)
                {
                    return await next();
                }
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    Guid transactionId;
                    using(var transaction = await _dbContext.BeginTransactionAsync())
                    {
                        _logger.LogInformation("----- Begin transaction {TransactionId} for {CommandName} ({@Command})", transaction.TransactionId, typeName, request);

                        response = await next();

                        _logger.LogInformation("----- Commit transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                        await _dbContext.CommitTransactionAsync(transaction);

                        transactionId = transaction.TransactionId;
                    }
                    await _orderingIntegrationEventService.PublishEventsThroughEventBusAsync(transactionId);
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", typeName, request);

                throw;
            }
        }
    }
}
