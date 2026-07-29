using Application.Operations.Alerts.Notifications;
using Application.Orders.Management.Automation;
using Domain.Operations.Entities;

namespace IceBot.UnitTests.Orders;

public sealed class FulfillmentReminderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OverdueItem_CreatesOneDeliveryPerRecipient()
    {
        var candidate = Candidate();
        var reminderStore = new FakeReminderStore(candidate, [Guid.NewGuid(), Guid.NewGuid()]);
        var deliveries = new FakeDeliveryStore();
        var service = new FulfillmentReminderService(reminderStore, deliveries);

        await service.ProcessAsync(candidate.OrderItemId, Now);

        Assert.Equal(2, deliveries.Items.Count);
        Assert.All(deliveries.Items, delivery => Assert.Equal("fulfillment_overdue", delivery.NotificationType));
    }

    [Fact]
    public async Task Retry_DoesNotCreateDuplicateDelivery()
    {
        var candidate = Candidate();
        var reminderStore = new FakeReminderStore(candidate, [Guid.NewGuid()]);
        var deliveries = new FakeDeliveryStore();
        var service = new FulfillmentReminderService(reminderStore, deliveries);

        await service.ProcessAsync(candidate.OrderItemId, Now);
        await service.ProcessAsync(candidate.OrderItemId, Now.AddSeconds(10));

        Assert.Single(deliveries.Items);
    }

    [Fact]
    public async Task ItemNoLongerOverdue_DoesNotCreateDelivery()
    {
        var deliveries = new FakeDeliveryStore();
        var service = new FulfillmentReminderService(new FakeReminderStore(null, [Guid.NewGuid()]), deliveries);

        await service.ProcessAsync(Guid.NewGuid(), Now);

        Assert.Empty(deliveries.Items);
    }

    private static FulfillmentReminderCandidate Candidate() => new(
        Guid.NewGuid(), Guid.NewGuid(), "ORDER-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Now.AddMinutes(-10), Now.AddMinutes(-5));

    private sealed class FakeReminderStore(
        FulfillmentReminderCandidate? candidate,
        IReadOnlyCollection<Guid> recipients) : IFulfillmentReminderStore
    {
        public Task<IReadOnlyList<Guid>> ListOverdueItemIdsAsync(DateTimeOffset observedAt, int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(candidate is null ? [] : [candidate.OrderItemId]);

        public Task<FulfillmentReminderCandidate?> GetOverdueCandidateAsync(Guid orderItemId,
            DateTimeOffset observedAt, CancellationToken cancellationToken = default) =>
            Task.FromResult(candidate?.OrderItemId == orderItemId ? candidate : null);

        public Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(Guid organizationId,
            Guid storeId, Guid kioskId, CancellationToken cancellationToken = default) =>
            Task.FromResult(recipients);
    }

    private sealed class FakeDeliveryStore : INotificationDeliveryStore
    {
        public List<NotificationDelivery> Items { get; } = [];

        public Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Items.Add(delivery);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByKeyAsync(string deliveryKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(item => item.DeliveryKey == deliveryKey));

        public Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTimeOffset now, DateTimeOffset processingStartedBefore,
            int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task<NotificationDelivery?> GetByOrganizationAsync(Guid organizationId, Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.OrganizationId == organizationId && item.Id == id));

        public Task AcquireLockAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) => action(cancellationToken);
    }
}
