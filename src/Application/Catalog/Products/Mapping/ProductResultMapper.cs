using Application.Catalog.Products.Results;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Mapping;

internal static class ProductResultMapper
{
    public static ProductResult ToResult(Product product)
    {
        return new ProductResult
        {
            Id = product.Id,
            OrganizationId = product.OrganizationId,
            StoreId = product.StoreId,
            KioskId = product.KioskId,
            TemplateProductId = product.TemplateProductId,
            CategoryId = product.CategoryId,
            Code = product.Code,
            Name = product.Name,
            DisplayName = product.DisplayName,
            Description = product.Description,
            ProductType = product.ProductType,
            BasePrice = product.BasePrice,
            Currency = product.Currency,
            IsAvailable = product.IsAvailable,
            PreparationTimeSeconds = product.PreparationTimeSeconds,
            ImageUrl = product.ImageUrl,
            ScopeType = product.ScopeType,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Variants = product.ProductVariants
                .OrderBy(variant => variant.DisplayOrder)
                .ThenBy(variant => variant.Name)
                .Select(ToVariantResult)
                .ToList(),
            OptionGroups = product.OptionGroups
                .OrderBy(group => group.DisplayOrder)
                .ThenBy(group => group.Name)
                .Select(group => ToOptionGroupResult(group, product.Currency))
                .ToList()
        };
    }

    public static OptionGroupResult ToOptionGroupResult(OptionGroup group, string currency)
    {
        return new OptionGroupResult
        {
            Id = group.Id,
            ProductId = group.ProductId,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            SelectionType = group.SelectionType,
            MinSelections = group.MinSelections,
            MaxSelections = group.MaxSelections,
            IsRequired = group.IsRequired,
            IsActive = group.IsActive,
            DisplayOrder = group.DisplayOrder,
            Options = group.ProductOptions.Where(option => option.DeletedAt == null)
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Name)
                .Select(option => ToProductOptionResult(option, currency))
                .ToList()
        };
    }

    public static ProductOptionResult ToProductOptionResult(ProductOption option, string currency) => new()
    {
        Id = option.Id,
        OptionGroupId = option.OptionGroupId,
        Code = option.Code,
        Name = option.Name,
        Description = option.Description,
        PriceDelta = option.PriceDelta,
        Currency = currency,
        IsDefault = option.IsDefault,
        IsAvailable = option.IsAvailable,
        DisplayOrder = option.DisplayOrder
    };

    public static ProductVariantResult ToVariantResult(ProductVariant variant)
    {
        return new ProductVariantResult
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Code = variant.Code,
            Name = variant.Name,
            DisplayName = variant.DisplayName,
            Description = variant.Description,
            VariantType = variant.VariantType,
            FulfillmentType = variant.FulfillmentType,
            SizeCode = variant.SizeCode,
            BasePrice = variant.BasePrice,
            Currency = variant.Currency,
            IsAvailable = variant.IsAvailable,
            DisplayOrder = variant.DisplayOrder,
            PreparationTimeSeconds = variant.PreparationTimeSeconds,
            ImageUrl = variant.ImageUrl,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };
    }
}
