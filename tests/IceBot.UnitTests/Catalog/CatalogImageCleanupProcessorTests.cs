using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Infrastructure.Catalog.Images;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IceBot.UnitTests.Catalog;

public sealed class CatalogImageCleanupProcessorTests
{
    [Fact]
    public async Task ProcessAsync_KeepsSharedAssetAndCompletesCleanupWithoutProviderDelete()
    {
        var cleanup = Cleanup();
        var store = Substitute.For<IProductStore>();
        var storage = Substitute.For<ICatalogImageStorage>();
        store.ListPendingCatalogImageCleanupsAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([cleanup]);
        store.IsCatalogImageAssetReferencedAsync(cleanup.CatalogImageAssetId, Arg.Any<CancellationToken>()).Returns(true);
        var processor = CreateProcessor(store, storage);

        var result = await processor.ProcessAsync(100);

        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.NotNull(cleanup.CompletedAt);
        Assert.Equal(CatalogImageAssetStatus.Active, cleanup.CatalogImageAsset.Status);
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeletesUnreferencedAssetAndMarksItDeleted()
    {
        var cleanup = Cleanup();
        var store = Substitute.For<IProductStore>();
        var storage = Substitute.For<ICatalogImageStorage>();
        store.ListPendingCatalogImageCleanupsAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([cleanup]);
        store.IsCatalogImageAssetReferencedAsync(cleanup.CatalogImageAssetId, Arg.Any<CancellationToken>()).Returns(false);
        var processor = CreateProcessor(store, storage);

        var result = await processor.ProcessAsync(100);

        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(CatalogImageAssetStatus.Deleted, cleanup.CatalogImageAsset.Status);
        Assert.NotNull(cleanup.CompletedAt);
        await storage.Received(1).DeleteAsync(cleanup.PublicIdSnapshot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_RetriesProviderFailureWithoutRestoringOwnerReference()
    {
        var cleanup = Cleanup();
        var store = Substitute.For<IProductStore>();
        var storage = Substitute.For<ICatalogImageStorage>();
        store.ListPendingCatalogImageCleanupsAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([cleanup]);
        store.IsCatalogImageAssetReferencedAsync(cleanup.CatalogImageAssetId, Arg.Any<CancellationToken>()).Returns(false);
        storage.DeleteAsync(cleanup.PublicIdSnapshot, Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("provider unavailable"));
        var processor = CreateProcessor(store, storage);

        var result = await processor.ProcessAsync(100);

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, cleanup.AttemptCount);
        Assert.Equal("CATALOG_IMAGE_DELETE_FAILED", cleanup.LastErrorCode);
        Assert.NotNull(cleanup.NextAttemptAt);
        Assert.Null(cleanup.CompletedAt);
        Assert.Equal(CatalogImageAssetStatus.Active, cleanup.CatalogImageAsset.Status);
    }

    private static CatalogImageCleanupProcessor CreateProcessor(IProductStore store, ICatalogImageStorage storage) =>
        new(store, storage, NullLogger<CatalogImageCleanupProcessor>.Instance);

    private static CatalogImageCleanup Cleanup()
    {
        var asset = new CatalogImageAsset
        {
            Id = Guid.NewGuid(),
            Provider = "Cloudinary",
            ProviderAssetId = Guid.NewGuid().ToString("N"),
            PublicId = "icebot/test/product/image",
            DeliveryUrl = "https://res.cloudinary.com/test/image/upload/v1/image.png",
            Version = 1,
            Format = "png",
            Width = 400,
            Height = 400,
            Bytes = 10
        };
        return new CatalogImageCleanup
        {
            CatalogImageAssetId = asset.Id,
            CatalogImageAsset = asset,
            PublicIdSnapshot = asset.PublicId
        };
    }
}
