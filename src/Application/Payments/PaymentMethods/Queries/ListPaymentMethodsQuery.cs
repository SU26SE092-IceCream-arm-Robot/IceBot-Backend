namespace Application.Payments.PaymentMethods.Queries;

public sealed class ListPaymentMethodsQuery
{
    public bool? ActiveOnly { get; init; }

    public ListPaymentMethodsQuery(bool? activeOnly = null)
    {
        ActiveOnly = activeOnly;
    }
}
