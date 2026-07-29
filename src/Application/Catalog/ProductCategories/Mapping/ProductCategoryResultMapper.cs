using Application.Catalog.ProductCategories.Results;
using Domain.Catalog.Entities;

namespace Application.Catalog.ProductCategories.Mapping;

public static class ProductCategoryResultMapper
{
    public static ProductCategoryResult ToResult(ProductCategory category) => new()
    {
        Id = category.Id,
        Code = category.Code,
        Name = category.Name,
        Description = category.Description,
        ProductType = category.ProductType,
        ImageUrl = category.ImageUrl,
        IsActive = category.IsActive,
        DisplayOrder = category.DisplayOrder
    };
}
