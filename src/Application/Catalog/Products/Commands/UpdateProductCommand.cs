using Application.Catalog.Products.Requests;
using System;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductCommand
{
    public Guid ProductId { get; init; }
    public UpdateProductRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
