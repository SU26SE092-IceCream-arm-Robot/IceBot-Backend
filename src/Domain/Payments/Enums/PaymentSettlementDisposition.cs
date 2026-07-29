namespace Domain.Payments.Enums;

public enum PaymentSettlementDisposition
{
    Unassigned = 0,
    Primary = 1,
    DuplicateRefundRequired = 2,
    DuplicateResolved = 3
}
