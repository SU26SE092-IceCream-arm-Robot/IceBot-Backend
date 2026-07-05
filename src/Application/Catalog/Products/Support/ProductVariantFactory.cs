using Application.Catalog.Products.Requests;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Support;

internal static class ProductVariantFactory
{
    public static ProductVariant CreateVariant(
        UpsertProductVariantRequest request,
        Guid productId,
        string currency,
        DateTimeOffset now,
        Guid? createdByAccountId,
        string? metadataJson = null)
    {
        return new ProductVariant
        {
            ProductId = productId,
            Code = ProductNormalizer.NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            DisplayName = ProductNormalizer.TrimToNull(request.DisplayName),
            Description = ProductNormalizer.TrimToNull(request.Description),
            VariantType = ProductNormalizer.NormalizeOptionalCode(request.VariantType, "Default"),
            FulfillmentType = request.FulfillmentType,
            SizeCode = ProductNormalizer.NormalizeNullableCode(request.SizeCode),
            BasePrice = request.BasePrice,
            Currency = ProductNormalizer.NormalizeCode(currency),
            IsAvailable = false,
            DisplayOrder = request.DisplayOrder,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = ProductNormalizer.TrimToNull(request.ImageUrl),
            MetadataJson = ProductNormalizer.TrimToNull(metadataJson),
            CreatedAt = now,
            CreatedByAccountId = createdByAccountId
        };
    }
}
