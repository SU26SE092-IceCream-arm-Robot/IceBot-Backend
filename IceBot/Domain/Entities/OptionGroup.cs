using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class OptionGroup : CatalogEntity
{
    public OptionSelectionType SelectionType { get; set; } = OptionSelectionType.Single;

    public int MinSelections { get; set; }

    public int MaxSelections { get; set; } = 1;

    public bool IsRequired { get; set; }

    public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
}
