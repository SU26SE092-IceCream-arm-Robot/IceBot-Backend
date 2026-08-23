using Application.Catalog.Products.Results;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;

namespace Application.Catalog.Images;

public static class CatalogImageDeliveryUrlFactory
{
    private const string CardTransformation = "c_fill,g_auto,w_640,h_640,f_auto,q_auto";
    private const string DetailTransformation = "c_fill,g_auto,w_1024,h_1024,f_auto,q_auto";

    public static CatalogImageResult? Create(CatalogImageAsset? asset, string? altText)
    {
        if (asset is null || asset.Status != CatalogImageAssetStatus.Active)
        {
            return null;
        }

        return new CatalogImageResult
        {
            AssetId = asset.Id,
            CardUrl = WithTransformation(asset.DeliveryUrl, CardTransformation),
            DetailUrl = WithTransformation(asset.DeliveryUrl, DetailTransformation),
            AltText = altText,
            Version = asset.Version
        };
    }

    private static string WithTransformation(string deliveryUrl, string transformation)
    {
        const string uploadSegment = "/upload/";
        var index = deliveryUrl.IndexOf(uploadSegment, StringComparison.Ordinal);
        return index < 0
            ? deliveryUrl
            : string.Concat(deliveryUrl.AsSpan(0, index + uploadSegment.Length), transformation, "/", deliveryUrl.AsSpan(index + uploadSegment.Length));
    }
}
