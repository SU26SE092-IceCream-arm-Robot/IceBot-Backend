using Application.Catalog.Products.Requests;
using System;

namespace Application.Catalog.Products.Commands;

public sealed class CreateProductCommand
{
    public CreateProductRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
