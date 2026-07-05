namespace Application.Catalog.Products.Requests;

public sealed class UpdateProductRequest
{
    public long? CategoryId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? ProductType { get; set; }

    public decimal? BasePrice { get; set; }

    public string? Currency { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }

}
