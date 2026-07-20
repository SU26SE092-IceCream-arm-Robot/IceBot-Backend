using Application.Operations.Alerts.Notifications;
using Application.Orders.Management.Automation;
using Domain.Operations.Entities;
using Infrastructure.Orders.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Infrastructure;

public sealed class BackgroundJobItemIsolationTests
{
    [Fact]
    public async Task FulfillmentReminderJob_PoisonItemDoesNotStopLaterItem()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var reminderStore = new PoisonFirstReminderStore(firstId, secondId);
        var services = new ServiceCollection()
            .AddSingleton<IFulfillmentReminderStore>(reminderStore)
            .AddSingleton<INotificationDeliveryStore, InMemoryTransactionDeliveryStore>()
            .AddTransient<FulfillmentReminderService>()
            .BuildServiceProvider();
        var job = new FulfillmentReminderJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new FulfillmentReminderOptions
            {
                Enabled = true,
                IntervalSeconds = 3600,
                BatchSize = 10
            }),
            NullLogger<FulfillmentReminderJob>.Instance);

        await job.StartAsync(CancellationToken.None);
        await reminderStore.SecondItemProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await job.StopAsync(CancellationToken.None);

        Assert.Equal([firstId, secondId], reminderStore.ProcessedIds);
    }

    private sealed class PoisonFirstReminderStore(Guid firstId, Guid secondId) : IFulfillmentReminderStore
    {
        public TaskCompletionSource SecondItemProcessed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<Guid> ProcessedIds { get; } = [];

        public Task<IReadOnlyList<Guid>> ListOverdueItemIdsAsync(
            DateTimeOffset observedAt,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([firstId, secondId]);

        public Task<FulfillmentReminderCandidate?> GetOverdueCandidateAsync(
            Guid orderItemId,
            DateTimeOffset observedAt,
            CancellationToken cancellationToken = default)
        {
            ProcessedIds.Add(orderItemId);
            if (orderItemId == firstId)
            {
                throw new InvalidOperationException("Poison item");
            }

            SecondItemProcessed.TrySetResult();
            return Task.FromResult<FulfillmentReminderCandidate?>(null);
        }

        public Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
            Guid organizationId,
            Guid storeId,
            Guid kioskId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>([]);
    }

    private sealed class InMemoryTransactionDeliveryStore : INotificationDeliveryStore
    {
        public Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ExistsByKeyAsync(string deliveryKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Guid>> ListDueIdsAsync(
            DateTimeOffset now,
            DateTimeOffset processingStartedBefore,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<NotificationDelivery?>(null);

        public Task<NotificationDelivery?> GetByOrganizationAsync(
            Guid organizationId,
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<NotificationDelivery?>(null);

        public Task AcquireLockAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }
}
