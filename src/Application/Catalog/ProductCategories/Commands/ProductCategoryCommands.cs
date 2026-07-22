using Application.Catalog.ProductCategories.Requests;

namespace Application.Catalog.ProductCategories.Commands;

public sealed record CreateProductCategoryCommand(CreateProductCategoryRequest Request, Guid? ActorId);
public sealed record UpdateProductCategoryCommand(long CategoryId, UpdateProductCategoryRequest Request, Guid? ActorId);
public sealed record SetProductCategoryStatusCommand(long CategoryId, bool IsActive, Guid? ActorId);
public sealed record DeleteProductCategoryCommand(long CategoryId);
