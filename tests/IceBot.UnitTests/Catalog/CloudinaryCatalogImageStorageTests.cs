using Application.Catalog.Images;
using Application.Shared.Exceptions;
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

    [Fact]
    public async Task UploadAsync_RejectsImageBelowMinimumDimensionsBeforeCallingCloudinary()
    {
        var storage = CreateStorage();

        var exception = await Assert.ThrowsAsync<CatalogImageUploadValidationException>(() => storage.UploadAsync(
            new CatalogImageStorageUpload(PngWithDimensions(399, 400), "image.png", "image/png",
                "organizations/test/products/test/image")));

        Assert.Equal(400, exception.StatusCode);
        Assert.Contains("dimensions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_WhenCloudinaryIsNotConfigured_ReturnsScopedServiceUnavailable()
    {
        var storage = new CloudinaryCatalogImageStorage(Options.Create(new CloudinaryCatalogImageStorageOptions()));

        var exception = await Assert.ThrowsAsync<AppException>(() => storage.UploadAsync(
            new CatalogImageStorageUpload(ValidPng(), "image.png", "image/png",
                "organizations/test/products/test/image")));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal("Catalog image storage is not configured.", exception.Message);
    }

    private static CloudinaryCatalogImageStorage CreateStorage() => new(Options.Create(new CloudinaryCatalogImageStorageOptions
    {
        CloudName = "test-cloud",
        ApiKey = "test-key",
        ApiSecret = "test-secret",
        RootFolder = "icebot/production"
    }));

    private static byte[] ValidPng() => PngWithDimensions(400, 400);

    private static byte[] PngWithDimensions(int width, int height) =>
    [
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 13,
        73, 72, 68, 82,
        (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
        (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height
    ];
}
