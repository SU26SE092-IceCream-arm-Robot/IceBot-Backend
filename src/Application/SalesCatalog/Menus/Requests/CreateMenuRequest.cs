using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class CreateMenuRequest
{
    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int DisplayOrder { get; set; }

}
