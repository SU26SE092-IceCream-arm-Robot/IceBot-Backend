using Application.Catalog.Abstractions;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Support;

namespace Application.Catalog.Products.Rules;

internal static class ProductVariantRequestValidator
{
    public static string? ValidateVariantValues(
        string code,
        string name,
        decimal basePrice,
        string currency,
        int? preparationTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Product variant code is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product variant name is required.";
        }

        if (basePrice < 0)
        {
            return "Product variant base price cannot be negative.";
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return "Product variant currency is required.";
        }

        if (preparationTimeSeconds < 0)
        {
            return "Product variant preparation time cannot be negative.";
        }

        return null;
    }

    public static async Task<string?> ValidateVariantFieldsAsync(
        IProductStore products,
        Guid productId,
        UpsertProductVariantRequest request,
        Guid? excludedVariantId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateVariantValues(
            request.Code,
            request.Name,
            request.BasePrice,
            request.Currency,
            request.PreparationTimeSeconds);

        if (validationError is not null)
        {
            return validationError;
        }

        if (await products.ProductVariantCodeExistsAsync(productId, ProductNormalizer.NormalizeCode(request.Code), excludedVariantId, cancellationToken))
        {
            return "Product variant code already exists for this product.";
        }

        return null;
    }
}
