using Application.Identity.Tokens.Claims;
using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed record GetKioskIngredientInventoriesQuery(Guid KioskId, CurrentUserContext UserContext);

public sealed class GetKioskIngredientInventoriesQueryHandler(IInventoryWorkspaceStore inventory)
{
    public async Task<ApiResult<IReadOnlyList<KioskIngredientInventoryResult>>> HandleAsync(GetKioskIngredientInventoriesQuery query, CancellationToken cancellationToken = default)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(query.KioskId, cancellationToken);
        if (kiosk is null) return ApiResult<IReadOnlyList<KioskIngredientInventoryResult>>.Fail("Kiosk was not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryView, query.UserContext, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id))
            return ApiResult<IReadOnlyList<KioskIngredientInventoryResult>>.Fail("Access denied.", 403);
        var items = await inventory.ListKioskIngredientInventoriesAsync(kiosk.Id, cancellationToken);
        return ApiResult<IReadOnlyList<KioskIngredientInventoryResult>>.Success(items.Select(KioskIngredientInventoryResultMapper.ToResult).ToArray());
    }
}
