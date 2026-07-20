using Application.Identity.Tokens.Claims;
using Application.ProductionPackages.Installation;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Domain.ProductionPackages;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Concurrency;
using Infrastructure.ProductionPackages;
using Infrastructure.RobotConfiguration.ArtifactContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IceBot.IntegrationTests.ProductionPackages;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ProductionPackageInstallationFlowIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public ProductionPackageInstallationFlowIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task InstallPublishedPackage_MaterializesCompleteDraftGraph_CopiesArtifact_AndIsIdempotent()
    {
        var actorId = Guid.NewGuid();
        var idempotencyKey = $"install-{Guid.NewGuid():N}";
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);

        var command = new InstallProductionPackageCommand
        {
            UserContext = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true },
            OrganizationId = scenario.OrganizationId,
            PackageId = scenario.PackageId,
            PackageVersionId = scenario.PackageVersionId,
            IdempotencyKey = idempotencyKey,
            ProductSourceKeys = ["ICE_CREAM"]
        };

        Guid installationId;
        Guid artifactId;
        string copiedStorageKey;
        await using (var installContext = _fixture.CreateDbContext())
        {
            var result = await CreateService(installContext, storage).InstallAsync(command, CancellationToken.None);

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(201, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(nameof(ProductionPackageInstallationStatus.Installed), result.Data.Status);
            Assert.NotNull(result.Data.DraftConfigurationReleaseId);
            installationId = result.Data.Id;

            var artifactMaterialization = Assert.Single(result.Data.Materializations,
                row => row.ResourceKind == nameof(ProductionPackageResourceKind.RobotArtifact));
            artifactId = Guid.Parse(artifactMaterialization.TargetKey);
        }

        await using (var assertionContext = _fixture.CreateDbContext())
        {
            var installation = await assertionContext.ProductionPackageInstallations.AsNoTracking()
                .Include(x => x.Materializations)
                .SingleAsync(x => x.Id == installationId);
            var product = await assertionContext.Products.AsNoTracking()
                .Include(x => x.ProductVariants).ThenInclude(x => x.Recipes)
                .SingleAsync(x => x.OrganizationId == scenario.OrganizationId && x.Code == "ICE_CREAM");
            var artifact = await assertionContext.RobotArtifacts.AsNoTracking()
                .SingleAsync(x => x.Id == artifactId);
            var program = await assertionContext.RobotPrograms.AsNoTracking()
                .Include(x => x.RobotProgramArtifacts)
                .SingleAsync(x => x.OrganizationId == scenario.OrganizationId);
            var composition = await assertionContext.ProductionCompositions.AsNoTracking()
                .SingleAsync(x => x.InstallationId == installationId);
            var release = await assertionContext.ConfigurationReleases.AsNoTracking()
                .Include(x => x.ExecutionRoutes)
                .SingleAsync(x => x.Id == installation.DraftConfigurationReleaseId);

            var variant = Assert.Single(product.ProductVariants);
            Assert.Single(variant.Recipes);
            Assert.Single(program.RobotProgramArtifacts);
            Assert.Equal(artifact.Id, program.RobotProgramArtifacts.Single().RobotArtifactId);
            Assert.Equal(program.Id, composition.GeneratedRobotProgramId);
            Assert.Equal(ProductionCompositionStatus.Applied, composition.Status);
            Assert.Single(release.ExecutionRoutes);
            Assert.Equal(Domain.ProductionConfiguration.Enums.ConfigurationReleaseStatus.Draft, release.Status);
            Assert.Equal(ProductionPackageInstallationStatus.Installed, installation.Status);
            Assert.Equal(6, installation.Materializations.Count);
            copiedStorageKey = artifact.StorageKey;
        }

        Assert.NotEqual(scenario.TemplateStorageKey, copiedStorageKey);
        Assert.True(await storage.ExistsAsync(copiedStorageKey));
        Assert.Equal(ProductionPackageInstallationScenarioSeed.ArtifactBytes,
            await storage.ReadBytesAsync(copiedStorageKey,
                ProductionPackageInstallationScenarioSeed.ArtifactBytes.LongLength));

        await using (var retryContext = _fixture.CreateDbContext())
        {
            var retry = await CreateService(retryContext, storage).InstallAsync(command, CancellationToken.None);

            Assert.True(retry.Succeeded, retry.Message);
            Assert.Equal(installationId, retry.Data!.Id);
            Assert.Equal(nameof(ProductionPackageInstallationStatus.Installed), retry.Data.Status);
        }

        await using (var finalContext = _fixture.CreateDbContext())
        {
            Assert.Equal(1, await finalContext.ProductionPackageInstallations.CountAsync(
                x => x.OrganizationId == scenario.OrganizationId && x.IdempotencyKey == idempotencyKey));
            Assert.Equal(1, await finalContext.Products.CountAsync(x => x.OrganizationId == scenario.OrganizationId));
            Assert.Equal(1, await finalContext.RobotArtifacts.CountAsync(x => x.OrganizationId == scenario.OrganizationId));
            Assert.Equal(1, await finalContext.RobotPrograms.CountAsync(x => x.OrganizationId == scenario.OrganizationId));
            Assert.Equal(1, await finalContext.ProductionCompositions.CountAsync(x => x.InstallationId == installationId));
            Assert.Equal(1, await finalContext.ConfigurationReleases.CountAsync(x => x.OrganizationId == scenario.OrganizationId));
        }
    }

    private static ProductionPackageInstallationService CreateService(
        global::Infrastructure.Data.IceBotDbContext dbContext,
        IArtifactObjectStorage storage)
    {
        var contentService = new ArtifactUploadContentService(
            storage,
            NullLogger<ArtifactUploadContentService>.Instance);
        return new ProductionPackageInstallationService(
            new ProductionPackageStore(dbContext),
            new ProductionPackageInstallationStore(dbContext),
            storage,
            contentService,
            new ArtifactPublicationValidator(
                new RobotArtifactTechnicalContractStore(dbContext),
                storage),
            new PostgresTechnicalResourceMutationCoordinator(dbContext));
    }

}
