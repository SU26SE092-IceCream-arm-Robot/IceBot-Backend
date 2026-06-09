using System;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductCommand
{
    public Guid ProductId { get; init; }
    public Guid? DeletedByAccountId { get; init; }
}
