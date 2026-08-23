using Application.Payments.PaymentSessions.Notifications;
using Domain.Identity.Enums;
using Infrastructure.Data;
using Infrastructure.Operations.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payments.Persistence;

public sealed class PaymentInterventionNotificationRecipientStore(IceBotDbContext db)
    : IPaymentInterventionNotificationRecipientStore
{
    public async Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
        => await OperationalBusinessNotificationRecipients.ListAsync(
            db, organizationId, storeId, kioskId, cancellationToken);
}
