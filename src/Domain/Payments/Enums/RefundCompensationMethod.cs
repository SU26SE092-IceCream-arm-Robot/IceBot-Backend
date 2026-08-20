namespace Domain.Payments.Enums;

/// <summary>
/// Describes how a completed refund compensates the customer. Voucher compensation
/// does not reverse the original money collection.
/// </summary>
public enum RefundCompensationMethod
{
    FullMoneyRefund = 1,
    Voucher = 2
}
