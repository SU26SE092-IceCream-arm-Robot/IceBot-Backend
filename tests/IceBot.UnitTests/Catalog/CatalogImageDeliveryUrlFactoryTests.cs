using Application.Catalog.Images;
using Domain.Catalog.Entities;

namespace IceBot.UnitTests.Catalog;

public sealed class CatalogImageDeliveryUrlFactoryTests
{
    [Fact]
    public void Create_InsertsPurposeSpecificTransformationsBeforeVersionedAssetPath()
    {
        var asset = new CatalogImageAsset
        {
            Id = Guid.NewGuid(),
            Provider = "Cloudinary",
            ProviderAssetId = "provider-asset",
            PublicId = "icebot/production/organizations/org/products/product/asset",
            DeliveryUrl = "https://res.cloudinary.com/example/image/upload/v42/icebot/production/asset.webp",
            Version = 42,
            Format = "webp",
            Width = 800,
            Height = 800,
            Bytes = 1024
        };

        var result = CatalogImageDeliveryUrlFactory.Create(asset, "Vanilla ice cream");

        Assert.NotNull(result);
        Assert.Equal(
            "https://res.cloudinary.com/example/image/upload/c_fill,g_auto,w_640,h_640,f_auto,q_auto/v42/icebot/production/asset.webp",
            result.CardUrl);
        Assert.Equal(
            "https://res.cloudinary.com/example/image/upload/c_fill,g_auto,w_1024,h_1024,f_auto,q_auto/v42/icebot/production/asset.webp",
            result.DetailUrl);
        Assert.Equal("Vanilla ice cream", result.AltText);
    }
}
