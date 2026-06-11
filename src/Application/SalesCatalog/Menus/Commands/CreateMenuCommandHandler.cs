using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class CreateMenuCommandHandler
{
    private readonly IMenuStore _menus;

    public CreateMenuCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuResult>> HandleAsync(
        CreateMenuCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

        var validationError = await MenuRequestValidator.ValidateMenuFieldsAsync(
            _menus,
            request.Code,
            request.Name,
            request.Currency,
            request.ScopeType,
            request.OrganizationId,
            request.StoreId,
            request.KioskId,
            request.EffectiveFrom,
            request.EffectiveTo,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuResult>.Fail(validationError);
        }

        var menu = new Menu
        {
            OrganizationId = request.OrganizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            Code = MenuNormalizer.NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            Description = MenuNormalizer.TrimToNull(request.Description),
            Status = request.Status,
            ScopeType = request.ScopeType,
            Currency = MenuNormalizer.NormalizeCode(request.Currency),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            DisplayOrder = request.DisplayOrder,
            MetadataSchemaVersion = Math.Max(request.MetadataSchemaVersion, 1),
            MetadataJson = MenuNormalizer.TrimToNull(request.MetadataJson),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _menus.AddMenuAsync(menu, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuResult>.Success(MenuResultMapper.ToResult(menu), "Menu created.", 201);
    }
}
