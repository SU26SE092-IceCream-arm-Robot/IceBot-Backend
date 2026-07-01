using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Commands;

public sealed record MenuManagementCommandScope(CurrentUserContext UserContext, Guid OrganizationId);

internal static class MenuManagementCommandRules
{
    public static ApiResult<T>? ValidateCreate<T>(
        MenuManagementCommandScope scope,
        TenantScopeType requestedScopeType,
        Guid? storeId,
        Guid? kioskId)
    {
        return requestedScopeType is not TenantScopeType.Global and not TenantScopeType.Device &&
               ScopeAccessRules.CanAccessScopedRow(
                   ScopeRoleSets.MenusManage,
                   scope.UserContext,
                   scope.OrganizationId,
                   storeId,
                   kioskId)
            ? null
            : ApiResult<T>.Fail("Access denied.", 403);
    }

    public static ApiResult<T>? ValidateExisting<T>(MenuManagementCommandScope scope, Menu menu)
    {
        return menu.OrganizationId == scope.OrganizationId &&
               menu.ScopeType != TenantScopeType.Global &&
               ScopeAccessRules.CanAccessScopedRow(
                   ScopeRoleSets.MenusManage,
                   scope.UserContext,
                   menu.OrganizationId,
                   menu.StoreId,
                   menu.KioskId)
            ? null
            : ApiResult<T>.Fail("Menu not found.", 404);
    }
}
