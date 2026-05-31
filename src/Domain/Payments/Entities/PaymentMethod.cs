using Domain.Common;

namespace Domain.Payments.Entities;

public partial class PaymentMethod : CatalogEntity
{
    public string Provider { get; set; } = "Internal";

    public string MethodType { get; set; } = "Unknown";

    public bool IsOnline { get; set; } = true;

    public int ConfigSchemaVersion { get; set; } = 1;

    public string? ConfigJson { get; set; }
}
