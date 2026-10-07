using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Infrastructure.Shared.Constants;

namespace Pos.tenant.Infrastructure.Shared.Workers
{
    public class SubscriptionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryWorker> _logger;
        private readonly SubscriptionExpiryOptions _options;

        public SubscriptionExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryWorker> logger,
            IOptions<SubscriptionExpiryOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(
                _options.PollingIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSubscriptionsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to scan subscriptions for expiry.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ProcessSubscriptionsAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Guid> subscriptionIds;

            // 1. Find a bounded batch and dispose the query scope.
            await using (var queryScope = _scopeFactory.CreateAsyncScope())
            {
                var repository = queryScope.ServiceProvider
                    .GetRequiredService<ITenantSubscriptionRepositoryAsync>();

                subscriptionIds =
                    await repository.GetSubscriptionIdsRequiringExpiryProcessingAsync(
                        DateTime.UtcNow,
                        _options.BatchSize,
                        cancellationToken);
            }

            // 2. Process each subscription independently.
            foreach (var subscriptionId in subscriptionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await using var processingScope =
                        _scopeFactory.CreateAsyncScope();

                    var expiryService = processingScope.ServiceProvider
                        .GetRequiredService<ISubscriptionExpiryService>();

                    await expiryService.ProcessExpiryAsync(
                        subscriptionId,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // Discard this scope. The next scan reloads current state.
                    _logger.LogError(
                        exception,
                        "Failed to process expiry for subscription {SubscriptionId}.",
                        subscriptionId);
                }
            }
        }
    }
}
