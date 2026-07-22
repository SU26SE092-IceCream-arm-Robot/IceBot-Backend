using Application.Identity.Tokens.Claims;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Diagnostics;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentSessionInterventionQueryTests
{
    [Fact]
    public async Task ManagerQueryUsesOnlyPaymentsManageScopeAndReturnsNoRawPayload()
    {
        var organizationId = Guid.NewGuid();
        var unrelatedKioskId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORDER-1",
            OrganizationId = organizationId,
            KioskId = Guid.NewGuid()
        };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "PayOS",
            ProviderOrderCode = "1234567890123",
            Status = PaymentTransactionStatus.Pending,
            Amount = 30_000,
            Currency = "VND",
            LastErrorCode = "AWAITING_SIGNED_WEBHOOK",
            RawResponseJson = "sensitive"
        };
        var store = Substitute.For<IPaymentStore>();
        store.CountPaymentSessionInterventionsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
                false,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(organizationId)),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Is<IReadOnlyCollection<Guid>>(ids => !ids.Contains(unrelatedKioskId)),
                Arg.Any<CancellationToken>())
            .Returns(1);
        store.ListPaymentSessionInterventionsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
                false,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(organizationId)),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Is<IReadOnlyCollection<Guid>>(ids => !ids.Contains(unrelatedKioskId)),
                1, 20,
                Arg.Any<CancellationToken>())
            .Returns(new[] { payment });
        var user = new CurrentUserContext
        {
            RoleScopes =
            [
                new UserRoleScope("Manager", organizationId, null, null),
                new UserRoleScope("Technician", null, null, unrelatedKioskId)
            ]
        };

        var result = await new ListPaymentSessionInterventionsQueryHandler(store).HandleAsync(
            new ListPaymentSessionInterventionsQuery(user, null, null, null, null, null));

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Data!);
        Assert.Equal(payment.Id, item.PaymentTransactionId);
        Assert.Equal("AWAITING_SIGNED_WEBHOOK", item.InterventionCode);
        Assert.DoesNotContain("Raw", item.GetType().GetProperties().Select(property => property.Name));
    }
}
