using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class CreateOptionGroupRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public OptionSelectionType SelectionType { get; set; } = OptionSelectionType.Single;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
