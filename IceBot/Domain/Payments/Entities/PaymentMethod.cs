using Domain.Common;

namespace Domain.Payments.Entities;

public partial class PaymentMethod : CatalogEntity
{
    public string Provider { get; set; } = "Internal";

    public string MethodType { get; set; } = "Unknown";

    public bool IsOnline { get; set; } = true;

    public string? ConfigJson { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
