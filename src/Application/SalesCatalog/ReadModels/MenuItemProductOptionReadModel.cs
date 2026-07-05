using Domain.Catalog.Enums;

namespace Application.SalesCatalog.ReadModels;

public sealed record MenuItemProductOptionReadModel(
    Guid MenuItemId,
    Guid ProductOptionId,
    long OptionGroupId,
    string OptionGroupCode,
    string OptionGroupName,
    OptionSelectionType SelectionType,
    int MinSelections,
    int MaxSelections,
    bool IsRequired,
    string Code,
    string Name,
    string? Description,
    decimal PriceDelta,
    bool IsAvailable,
    bool IsDefault,
    int DisplayOrder);
