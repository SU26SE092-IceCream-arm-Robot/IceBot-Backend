using Domain.Catalog.Enums;
using Domain.Common;

namespace Domain.Catalog.Entities;

public partial class OptionGroup : CatalogEntity
{
    public Guid ProductId { get; set; }

    public OptionSelectionType SelectionType { get; set; } = OptionSelectionType.Single;

    public int MinSelections { get; set; }

    public int MaxSelections { get; set; } = 1;

    public bool IsRequired { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
}
