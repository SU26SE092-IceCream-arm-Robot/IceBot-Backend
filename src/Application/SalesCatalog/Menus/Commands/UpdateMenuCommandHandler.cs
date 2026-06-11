using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuCommandHandler
{
    private readonly IMenuStore _menus;

    public UpdateMenuCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuResult>> HandleAsync(
        UpdateMenuCommand command,
        CancellationToken cancellationToken = default)
    {
        var menuId = command.MenuId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? menu.Code : MenuNormalizer.NormalizeCode(request.Code);
        var newName = string.IsNullOrWhiteSpace(request.Name) ? menu.Name : request.Name;
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? menu.Currency : request.Currency;
        var newScopeType = request.ScopeType ?? menu.ScopeType;
        var newOrganizationId = request.OrganizationId ?? menu.OrganizationId;
        var newStoreId = request.StoreId ?? menu.StoreId;
        var newKioskId = request.KioskId ?? menu.KioskId;
        var newEffectiveFrom = request.EffectiveFrom ?? menu.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? menu.EffectiveTo;

        var validationError = await MenuRequestValidator.ValidateMenuFieldsAsync(
            _menus,
            newCode,
            newName,
            newCurrency,
            newScopeType,
            newOrganizationId,
            newStoreId,
            newKioskId,
            newEffectiveFrom,
            newEffectiveTo,
            menu.Id,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuResult>.Fail(validationError);
        }

        menu.OrganizationId = newOrganizationId;
        menu.StoreId = newStoreId;
        menu.KioskId = newKioskId;
        menu.Code = newCode;
        menu.Name = newName.Trim();
        menu.Description = request.Description is null ? menu.Description : MenuNormalizer.TrimToNull(request.Description);
        menu.Status = request.Status ?? menu.Status;
        menu.ScopeType = newScopeType;
        menu.Currency = MenuNormalizer.NormalizeCode(newCurrency);
        menu.EffectiveFrom = newEffectiveFrom;
        menu.EffectiveTo = newEffectiveTo;
        menu.DisplayOrder = request.DisplayOrder ?? menu.DisplayOrder;
        menu.MetadataSchemaVersion = request.MetadataSchemaVersion.HasValue
            ? Math.Max(request.MetadataSchemaVersion.Value, 1)
            : menu.MetadataSchemaVersion;
        menu.MetadataJson = request.MetadataJson is null ? menu.MetadataJson : MenuNormalizer.TrimToNull(request.MetadataJson);
        menu.UpdatedAt = DateTimeOffset.UtcNow;
        menu.UpdatedByAccountId = updatedByAccountId;

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuResult>.Success(MenuResultMapper.ToResult(menu), "Menu updated.");
    }
}
