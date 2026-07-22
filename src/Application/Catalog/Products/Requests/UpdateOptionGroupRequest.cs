using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class UpdateOptionGroupRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public OptionSelectionType SelectionType { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
