namespace Application.SalesCatalog.Menus.Requests;

public sealed class UpdateMenuRequest
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Currency { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int? DisplayOrder { get; set; }

}
