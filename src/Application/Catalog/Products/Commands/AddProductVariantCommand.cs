using Application.Catalog.Products.Requests;
using System;

namespace Application.Catalog.Products.Commands;

public sealed class AddProductVariantCommand
{
    public Guid ProductId { get; init; }
    public UpsertProductVariantRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
