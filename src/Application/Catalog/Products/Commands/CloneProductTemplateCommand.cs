using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class CloneProductTemplateCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public required CloneProductTemplateRequest Request { get; init; }
}
