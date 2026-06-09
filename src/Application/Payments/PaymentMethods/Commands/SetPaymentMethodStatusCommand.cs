namespace Application.Payments.PaymentMethods.Commands;

public sealed class SetPaymentMethodStatusCommand
{
    public long Id { get; init; }
    public bool IsActive { get; init; }

    public SetPaymentMethodStatusCommand(long id, bool isActive)
    {
        Id = id;
        IsActive = isActive;
    }
}
