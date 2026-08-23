using Application.Catalog.Images;
using Infrastructure.Catalog.Images;
using Microsoft.Extensions.Options;

namespace IceBot.UnitTests.Catalog;

public sealed class CloudinaryCatalogImageStorageTests
{
    [Fact]
    public async Task UploadAsync_RejectsPublicIdThatAlreadyContainsRootFolder()
    {
        var storage = CreateStorage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => storage.UploadAsync(
            new CatalogImageStorageUpload(ValidPng(), "image.png", "image/png",
                "icebot/production/organizations/test/products/test/image")));

        Assert.Contains("must not include", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAsync_RejectsInvalidImageContentAsClientValidationFailure()
    {
        var storage = CreateStorage();

        var exception = await Assert.ThrowsAsync<CatalogImageUploadValidationException>(() => storage.UploadAsync(
            new CatalogImageStorageUpload([0x00], "image.png", "image/png",
                "organizations/test/products/test/image")));

        Assert.Equal(400, exception.StatusCode);
    }

    private static CloudinaryCatalogImageStorage CreateStorage() => new(Options.Create(new CloudinaryCatalogImageStorageOptions
    {
        CloudName = "test-cloud",
        ApiKey = "test-key",
        ApiSecret = "test-secret",
        RootFolder = "icebot/production"
    }));

    private static byte[] ValidPng() => [137, 80, 78, 71, 13, 10, 26, 10];
}
