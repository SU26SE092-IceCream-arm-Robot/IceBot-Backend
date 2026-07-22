using Domain.Catalog.Enums;

namespace Application.SalesCatalog.ReadModels;

public sealed record MenuItemOptionGroupReadModel(
    Guid MenuItemId,
    long OptionGroupId,
    string OptionGroupCode,
    string OptionGroupName,
    OptionSelectionType SelectionType,
    int MinSelections,
    int MaxSelections,
    bool IsRequired);
