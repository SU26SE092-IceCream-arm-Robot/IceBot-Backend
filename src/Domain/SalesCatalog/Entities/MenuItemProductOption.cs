using Domain.Common;

namespace Domain.SalesCatalog.Entities;

public class MenuItemProductOption : BusinessEntity
{
    public Guid MenuItemId { get; set; }

    public Guid ProductOptionId { get; set; }

    public virtual MenuItem MenuItem { get; set; } = null!;
}
