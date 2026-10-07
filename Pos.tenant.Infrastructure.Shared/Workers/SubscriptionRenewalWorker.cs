using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Infrastructure.Shared.Constants;

namespace Pos.tenant.Infrastructure.Shared.Workers
{
    public class SubscriptionRenewalWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionRenewalWorker> _logger;
        private readonly SubscriptionRenewalOptions _options;

        public SubscriptionRenewalWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionRenewalWorker> logger,
            IOptions<SubscriptionRenewalOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CreateRenewalInvoicesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to scan subscriptions for renewal.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task CreateRenewalInvoicesAsync(CancellationToken cancellationToken)
        {
            // Read a bounded batch, then dispose the query scope before processing.
            IReadOnlyList<Guid> subscriptionIds;
            var renewBeforeUtc = DateTime.UtcNow.AddDays(_options.AdvanceDays);

            await using (var queryScope = _scopeFactory.CreateAsyncScope())
            {
                var repository = queryScope.ServiceProvider
                    .GetRequiredService<ITenantSubscriptionRepositoryAsync>();

                subscriptionIds = await repository.GetSubscriptionIdsDueForRenewalAsync(
                    renewBeforeUtc, _options.BatchSize, cancellationToken);
            }

            foreach (var subscriptionId in subscriptionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Each subscription has its own service, repositories and DbContext.
                    await using var renewalScope = _scopeFactory.CreateAsyncScope();
                    var renewalService = renewalScope.ServiceProvider
                        .GetRequiredService<ISubscriptionRenewalService>();

                    var result = await renewalService.CreateRenewalInvoiceAsync(
                        subscriptionId, cancellationToken);

                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Renewal invoice was not created for subscription {SubscriptionId}: {Errors}",
                            subscriptionId, string.Join("; ", result.Errors));
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // A failed save must not leak tracked changes into the next subscription.
                    _logger.LogError(exception,
                        "Failed to create renewal invoice for subscription {SubscriptionId}.",
                        subscriptionId);
                }
            }
        }
    }
}
