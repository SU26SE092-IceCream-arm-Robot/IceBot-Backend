namespace Application.SalesCatalog.RuntimeMenus.Results;

public sealed record RuntimeMenuProjection(
    string Revision,
    List<RuntimeMenuItemResult> Items);

public sealed record RuntimeMenuCachedProjection(
    string Revision,
    List<RuntimeMenuItemResult> Items,
    DateTimeOffset ValidUntil);
