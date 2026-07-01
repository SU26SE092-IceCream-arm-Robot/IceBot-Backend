using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Commands;

public sealed record ProductManagementCommandScope(
    CurrentUserContext UserContext,
    Guid? OrganizationId,
    bool IsGlobalTemplate = false);

internal static class ProductManagementCommandRules
{
    public static ApiResult<T>? ValidateCreate<T>(
        ProductManagementCommandScope scope,
        TenantScopeType requestedScopeType,
        Guid? storeId,
        Guid? kioskId)
    {
        if (scope.IsGlobalTemplate)
        {
            return scope.UserContext.IsSystemAdmin &&
                   requestedScopeType == TenantScopeType.Global &&
                   storeId is null && kioskId is null
                ? null
                : ApiResult<T>.Fail("Global product templates require SystemAdmin and Global scope.", 403);
        }

        if (!scope.OrganizationId.HasValue ||
            requestedScopeType is TenantScopeType.Global or TenantScopeType.Device ||
            !ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ProductsManage,
                scope.UserContext,
                scope.OrganizationId,
                storeId,
                kioskId))
        {
            return ApiResult<T>.Fail("Access denied.", 403);
        }

        return null;
    }

    public static ApiResult<T>? ValidateExisting<T>(ProductManagementCommandScope scope, Product product)
    {
        if (scope.IsGlobalTemplate)
        {
            return scope.UserContext.IsSystemAdmin &&
                   product.ScopeType == TenantScopeType.Global &&
                   product.OrganizationId is null && product.StoreId is null && product.KioskId is null
                ? null
                : ApiResult<T>.Fail("Product template not found.", 404);
        }

        return scope.OrganizationId.HasValue &&
               product.OrganizationId == scope.OrganizationId &&
               product.ScopeType != TenantScopeType.Global &&
               ScopeAccessRules.CanAccessScopedRow(
                   ScopeRoleSets.ProductsManage,
                   scope.UserContext,
                   product.OrganizationId,
                   product.StoreId,
                   product.KioskId)
            ? null
            : ApiResult<T>.Fail("Product not found.", 404);
    }
}
