using Application.Orders.Management.Results;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Rules;

public static class FulfillmentSlaRules
{
    private static readonly TimeSpan DueSoonWindow = TimeSpan.FromMinutes(2);

    public static FulfillmentSlaProjection Project(
        DateTimeOffset? paidAt,
        int? preparationTimeSeconds,
        OrderItemStatus itemStatus,
        DateTimeOffset observedAt)
    {
        if (!paidAt.HasValue || !preparationTimeSeconds.HasValue || preparationTimeSeconds.Value <= 0)
        {
            return new FulfillmentSlaProjection(null, FulfillmentSlaStatus.NotConfigured);
        }

        var expectedReadyAt = paidAt.Value.AddSeconds(preparationTimeSeconds.Value);
        if (itemStatus is OrderItemStatus.Completed or OrderItemStatus.Cancelled)
        {
            return new FulfillmentSlaProjection(expectedReadyAt, FulfillmentSlaStatus.Terminal);
        }

        if (observedAt >= expectedReadyAt)
        {
            return new FulfillmentSlaProjection(expectedReadyAt, FulfillmentSlaStatus.Overdue);
        }

        return new FulfillmentSlaProjection(
            expectedReadyAt,
            expectedReadyAt - observedAt <= DueSoonWindow
                ? FulfillmentSlaStatus.DueSoon
                : FulfillmentSlaStatus.OnTrack);
    }
}

public sealed record FulfillmentSlaProjection(
    DateTimeOffset? ExpectedReadyAt,
    FulfillmentSlaStatus Status);
