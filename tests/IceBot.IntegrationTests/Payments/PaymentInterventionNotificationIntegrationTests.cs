using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Notifications;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Operations.Persistence;
using Infrastructure.Payments.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Payments;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class PaymentInterventionNotificationIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ScopedStaffRecipient_ReceivesOneDeliveryForRepeatedIntervention()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"PAY-NOTIFY-{Guid.NewGuid():N}",
            Name = "Payment notification organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Payment notification store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Payment notification kiosk",
            Status = KioskStatus.Active
        };
        var account = new Account
        {
            UserName = $"payment-staff-{Guid.NewGuid():N}",
            Email = $"payment-staff-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        account.NotificationDevices.Add(new AccountNotificationDevice
        {
            AccountId = account.Id,
            InstallationId = Guid.NewGuid(),
            Platform = "Android",
            PushToken = $"token-{Guid.NewGuid():N}",
            PushTokenHash = $"hash-{Guid.NewGuid():N}"
        });
        var staffRole = await db.Roles.SingleOrDefaultAsync(x => x.Code == "Staff");
        if (staffRole is null)
        {
            staffRole = new Role { Code = "Staff", Name = "Staff", IsSystemRole = true };
            db.Roles.Add(staffRole);
            await db.SaveChangesAsync();
        }

        db.AddRange(organization, store, kiosk, account);
        await db.SaveChangesAsync();
        db.AccountRoles.Add(new AccountRole
        {
            AccountId = account.Id,
            RoleId = staffRole.Id,
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            OrderNumber = $"ORDER-{Guid.NewGuid():N}"
        };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "PayOS",
            LastErrorCode = "AWAITING_SIGNED_WEBHOOK"
        };
        var deliveryStore = new NotificationDeliveryStore(db);
        var notifier = new PaymentInterventionNotifier(
            new PaymentInterventionNotificationRecipientStore(db), deliveryStore);

        await notifier.NotifyIfRequiredAsync(
            payment, PaymentSessionReconciliationOutcome.AwaitingWebhook, DateTimeOffset.UtcNow);
        await deliveryStore.SaveChangesAsync();
        await notifier.NotifyIfRequiredAsync(
            payment, PaymentSessionReconciliationOutcome.AwaitingWebhook, DateTimeOffset.UtcNow);
        await deliveryStore.SaveChangesAsync();

        Assert.Equal(1, await db.NotificationDeliveries.CountAsync(delivery =>
            delivery.NotificationType == "payment_intervention" &&
            delivery.SubjectId == payment.Id &&
            delivery.RecipientAccountId == account.Id));
    }
}
