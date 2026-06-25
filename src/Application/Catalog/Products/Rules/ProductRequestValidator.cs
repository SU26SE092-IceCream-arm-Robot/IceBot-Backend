using Application.Catalog.Abstractions;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Support;
using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Rules;

internal static class ProductRequestValidator
{
    public static string? ValidateBasicFields(
        string code,
        string name,
        decimal basePrice,
        string currency,
        int? preparationTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Product code is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (basePrice < 0)
        {
            return "Product base price cannot be negative.";
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return "Currency is required.";
        }

        if (preparationTimeSeconds < 0)
        {
            return "Preparation time cannot be negative.";
        }

        return null;
    }

    public static async Task<string?> ValidateProductFieldsAsync(
        IProductStore products,
        string code,
        string name,
        decimal basePrice,
        string currency,
        int? preparationTimeSeconds,
        TenantScopeType scopeType,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        long? categoryId,
        Guid? excludedProductId,
        CancellationToken cancellationToken)
    {
        var basicError = ValidateBasicFields(code, name, basePrice, currency, preparationTimeSeconds);
        if (basicError is not null)
        {
            return basicError;
        }

        var scopeError = ProductTenantScopeRules.ValidateTenantScope(scopeType, organizationId, storeId, kioskId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        if (categoryId.HasValue && !await products.ProductCategoryExistsAsync(categoryId.Value, cancellationToken))
        {
            return "Product category does not exist.";
        }

        if (await products.ProductCodeExistsAsync(
            organizationId,
            storeId,
            kioskId,
            ProductNormalizer.NormalizeCode(code),
            excludedProductId,
            cancellationToken))
        {
            return "Product code already exists in this scope.";
        }

        return null;
    }

    public static async Task<string?> ValidateCreateRequestAsync(
        IProductStore products,
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateProductFieldsAsync(
            products,
            request.Code,
            request.Name,
            request.BasePrice,
            request.Currency,
            request.PreparationTimeSeconds,
            request.ScopeType,
            request.OrganizationId,
            request.StoreId,
            request.KioskId,
            request.CategoryId,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return validationError;
        }

        var variantCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variant in request.Variants)
        {
            var variantError = ProductVariantRequestValidator.ValidateVariantValues(
                variant.Code,
                variant.Name,
                variant.BasePrice,
                variant.Currency,
                variant.PreparationTimeSeconds,
                variant.FulfillmentType);

            if (variantError is not null)
            {
                return variantError;
            }

            if (!variantCodes.Add(ProductNormalizer.NormalizeCode(variant.Code)))
            {
                return $"Duplicate variant code '{variant.Code}'.";
            }
        }

        return null;
    }
}
