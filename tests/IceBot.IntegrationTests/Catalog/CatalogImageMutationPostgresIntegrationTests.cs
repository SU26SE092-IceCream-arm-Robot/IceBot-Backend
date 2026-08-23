using Application.Catalog.Images;
using Application.Catalog.Products.Commands;
using Application.Identity.Tokens.Claims;
using Domain.Catalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Catalog.Images;
using Infrastructure.Catalog.Persistence;
using Infrastructure.Concurrency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace IceBot.IntegrationTests.Catalog;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class CatalogImageMutationPostgresIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ReplaceProductImage_ReplaysSameIdempotencyKeyWithoutSecondUpload()
    {
        var product = await SeedProductAsync();
        var storage = new RecordingCatalogImageStorage();
        await using var db = fixture.CreateDbContext();
        var handler = CreateHandler(db, storage);
        var scope = SystemAdminScope(product.OrganizationId!.Value);

        var first = await handler.ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [1, 2, 3], "vanilla.png", "image/png", "replace-once", null, default);
        var replay = await handler.ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [1, 2, 3], "vanilla.png", "image/png", "replace-once", null, default);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal(first.Data!.Image!.AssetId, replay.Data!.Image!.AssetId);
        Assert.Equal(1, storage.UploadCount);

        await using var assertion = fixture.CreateDbContext();
        Assert.Single(await assertion.CatalogImageOperationReplays
            .Where(item => item.OwnerId == product.Id)
            .ToListAsync());
    }

    [IntegrationFact]
    public async Task ReplaceProductImage_ReplayReturnsCurrentAuthoritativeProjection()
    {
        var product = await SeedProductAsync();
        var storage = new RecordingCatalogImageStorage();
        await using var db = fixture.CreateDbContext();
        var handler = CreateHandler(db, storage);
        var scope = SystemAdminScope(product.OrganizationId!.Value);

        var first = await handler.ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [1, 2, 3], "vanilla.png", "image/png", "replay-current", null, default);
        Assert.True(first.Succeeded, first.Message);

        var persisted = await db.Products.SingleAsync(item => item.Id == product.Id);
        persisted.Name = "Renamed after image replacement";
        await db.SaveChangesAsync();

        var replay = await handler.ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [1, 2, 3], "vanilla.png", "image/png", "replay-current", null, default);

        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal("Renamed after image replacement", replay.Data!.Name);
        Assert.Equal(1, storage.UploadCount);
    }

    [IntegrationFact]
    public async Task ConcurrentReplaceProductImage_AllowsOneRevisionCommitAndOneProviderUpload()
    {
        var product = await SeedProductAsync();
        var storage = new RecordingCatalogImageStorage();
        var scope = SystemAdminScope(product.OrganizationId!.Value);

        await using var firstDb = fixture.CreateDbContext();
        await using var secondDb = fixture.CreateDbContext();
        var first = CreateHandler(firstDb, storage).ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [1, 2, 3], "vanilla.png", "image/png", "replace-one", null, default);
        var second = CreateHandler(secondDb, storage).ReplaceProductAsync(
            scope, product.Id, 1, "Vanilla", [4, 5, 6], "vanilla.png", "image/png", "replace-two", null, default);

        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Equal(1, results.Count(result => !result.Succeeded && result.StatusCode == 409));
        Assert.Equal(1, storage.UploadCount);

        await using var assertion = fixture.CreateDbContext();
        var persisted = await assertion.Products.SingleAsync(item => item.Id == product.Id);
        Assert.Equal(2, persisted.Revision);
        Assert.Single(await assertion.CatalogImageAssets
            .Where(asset => asset.PublicId.Contains(product.Id.ToString()))
            .ToListAsync());
    }

    private ReplaceCatalogImageCommandHandler CreateHandler(
        global::Infrastructure.Data.IceBotDbContext db,
        RecordingCatalogImageStorage storage) =>
        new(new ProductStore(db), storage, CreateMutationCoordinator(),
            NullLogger<ReplaceCatalogImageCommandHandler>.Instance);

    private PostgresCatalogImageMutationCoordinator CreateMutationCoordinator() =>
        new(new PostgresAdvisoryLockManager(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CONNECTIONSTRING"] = fixture.ConnectionString
            })
            .Build()));

    private async Task<Product> SeedProductAsync()
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Catalog image test organization"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}",
            Name = "Catalog image test product",
            Currency = "VND",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var db = fixture.CreateDbContext();
        db.AddRange(organization, product);
        await db.SaveChangesAsync();
        return product;
    }

    private static ProductManagementCommandScope SystemAdminScope(Guid organizationId) =>
        new(new CurrentUserContext { IsSystemAdmin = true }, organizationId);

    private sealed class RecordingCatalogImageStorage : ICatalogImageStorage
    {
        private readonly ConcurrentQueue<CatalogImageStorageUpload> _uploads = new();

        public int UploadCount => _uploads.Count;

        public Task<CatalogImageStorageResult> UploadAsync(
            CatalogImageStorageUpload upload,
            CancellationToken cancellationToken = default)
        {
            _uploads.Enqueue(upload);
            var providerAssetId = Guid.NewGuid().ToString("N");
            return Task.FromResult(new CatalogImageStorageResult(
                "Cloudinary",
                providerAssetId,
                upload.PublicId,
                $"https://res.cloudinary.com/test/image/upload/v1/{upload.PublicId}.png",
                1,
                "png",
                400,
                400,
                upload.Content.Length));
        }

        public Task DeleteAsync(string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
