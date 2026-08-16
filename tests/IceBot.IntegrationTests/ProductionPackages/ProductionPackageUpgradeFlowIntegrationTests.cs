using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Application.ProductionConfiguration.Deployments.Results;
using Application.ProductionPackages.Installation;
using Application.ProductionPackages.Upgrades;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.ProductionPackages;
using Domain.Devices.ExecutionEndpoints;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Concurrency;
using Infrastructure.ProductionPackages;
using Infrastructure.RobotConfiguration.ArtifactContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IceBot.IntegrationTests.ProductionPackages;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ProductionPackageUpgradeFlowIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public ProductionPackageUpgradeFlowIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task UpgradeHttpContract_PreviewsExecutesListsAndReadsOperationalDetail()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"http-upgrade-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            var target = await CloneAsNextPublishedVersionAsync(
                setupContext, scenario.PackageVersionId, actorId);
            await setupContext.SaveChangesAsync();
            scenario = scenario with { PackageVersionId = target.Id };
        }

        await using var factory = new PackageApiWebApplicationFactory(_fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var basePath = $"/api/v1/management/organizations/{scenario.OrganizationId:D}/" +
                       $"production-package-installations/{installationId:D}/upgrades";
        string previewChecksum;
        using (var previewResponse = await client.PostAsJsonAsync($"{basePath}/preview", new
               {
                   TargetPackageVersionId = scenario.PackageVersionId,
                   ProductSourceKeys = new[] { scenario.ProductSourceKey }
               }))
        {
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
            using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
            previewChecksum = preview.RootElement.GetProperty("data").GetProperty("previewChecksum").GetString()!;
            Assert.Equal("ReuseExistingCandidate", preview.RootElement.GetProperty("data")
                .GetProperty("artifacts")[0].GetProperty("materializationAction").GetString());
        }

        Guid upgradeId;
        var executeRequest = new HttpRequestMessage(HttpMethod.Post, basePath)
        {
            Content = JsonContent.Create(new
            {
                TargetPackageVersionId = scenario.PackageVersionId,
                PreviewChecksum = previewChecksum,
                ProductSourceKeys = new[] { scenario.ProductSourceKey }
            })
        };
        executeRequest.Headers.Add("Idempotency-Key", $"http-upgrade-{Guid.NewGuid():N}");
        using (var executeResponse = await client.SendAsync(executeRequest))
        {
            Assert.Equal(HttpStatusCode.Created, executeResponse.StatusCode);
            using var executed = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
            upgradeId = executed.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        }

        using (var listResponse = await client.GetAsync($"{basePath}?status=ReadyForReview"))
        {
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, list.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        }
        using (var detailResponse = await client.GetAsync($"{basePath}/{upgradeId:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
            using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
            var data = detail.RootElement.GetProperty("data");
            Assert.Equal(upgradeId, data.GetProperty("summary").GetProperty("id").GetGuid());
            Assert.True(data.TryGetProperty("menuChanges", out _));
            Assert.True(data.TryGetProperty("endpoints", out _));
        }

        Guid targetInstallationId;
        Guid targetReleaseId;
        await using (var deploymentSetupContext = _fixture.CreateDbContext())
        {
            var upgrade = await deploymentSetupContext.ProductionPackageUpgrades.AsNoTracking()
                .SingleAsync(item => item.Id == upgradeId);
            targetInstallationId = upgrade.TargetInstallationId!.Value;
            targetReleaseId = await deploymentSetupContext.ProductionPackageInstallations.AsNoTracking()
                .Where(item => item.Id == targetInstallationId)
                .Select(item => item.DraftConfigurationReleaseId!.Value)
                .SingleAsync();
        }
        await ActivateFullEdgeAsync(_fixture, scenario.ExecutionEndpointId, Guid.NewGuid(),
            targetReleaseId, new string('d', 64));
        await using (var displacedDeploymentContext = _fixture.CreateDbContext())
        {
            await displacedDeploymentContext.KioskExecutionEndpoints
                .Where(item => item.Id == scenario.ExecutionEndpointId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.ActiveConfigurationDeploymentId, (Guid?)null)
                    .SetProperty(item => item.ActiveConfigurationReleaseId, (Guid?)null)
                    .SetProperty(item => item.ActiveConfigurationReleaseChecksum, (string?)null));
        }
        using (var cutoverResponse = await client.PostAsync($"{basePath}/{upgradeId:D}/cutover", null))
            Assert.Equal(HttpStatusCode.Conflict, cutoverResponse.StatusCode);
        using (var rollbackWithoutReason = await client.PostAsJsonAsync(
                   $"{basePath}/{upgradeId:D}/rollback", new { }))
            Assert.Equal(HttpStatusCode.BadRequest, rollbackWithoutReason.StatusCode);

        using (var abandonWithoutReason = await client.PostAsJsonAsync(
                   $"{basePath}/{upgradeId:D}/abandon", new { Reason = " " }))
            Assert.Equal(HttpStatusCode.BadRequest, abandonWithoutReason.StatusCode);
        using (var abandonResponse = await client.PostAsJsonAsync(
                   $"{basePath}/{upgradeId:D}/abandon", new { Reason = "Successor rejected during review." }))
        {
            Assert.Equal(HttpStatusCode.OK, abandonResponse.StatusCode);
            using var abandoned = JsonDocument.Parse(await abandonResponse.Content.ReadAsStringAsync());
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.Abandoned),
                abandoned.RootElement.GetProperty("data").GetProperty("status").GetString());
        }
        using (var abandonRetry = await client.PostAsJsonAsync(
                   $"{basePath}/{upgradeId:D}/abandon", new { Reason = "Retry must be idempotent." }))
            Assert.Equal(HttpStatusCode.OK, abandonRetry.StatusCode);

        await using (var assertionContext = _fixture.CreateDbContext())
        {
            var upgrade = await assertionContext.ProductionPackageUpgrades.AsNoTracking()
                .SingleAsync(item => item.Id == upgradeId);
            var source = await assertionContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(item => item.Id == installationId);
            var target = await assertionContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(item => item.Id == upgrade.TargetInstallationId);
            var materializations = await assertionContext.ProductionPackageMaterializations.AsNoTracking()
                .Where(item => item.InstallationId == target.Id).ToArrayAsync();

            Assert.Equal(ProductionPackageUpgradeStatus.Abandoned, upgrade.Status);
            Assert.Equal("Successor rejected during review.", upgrade.AbandonReason);
            Assert.Equal(ProductionPackageInstallationStatus.Installed, source.Status);
            Assert.Equal(ProductionPackageInstallationStatus.Abandoned, target.Status);
            Assert.NotEmpty(materializations);

            var rootIds = materializations
                .Where(item => item.ResourceKind is ProductionPackageResourceKind.Product or
                    ProductionPackageResourceKind.RobotProgram or
                    ProductionPackageResourceKind.ConfigurationRelease)
                .Select(item => Guid.Parse(item.TargetKey)).ToHashSet();
            Assert.All(await assertionContext.Products.IgnoreQueryFilters()
                    .Where(item => rootIds.Contains(item.Id)).ToArrayAsync(),
                item => Assert.NotNull(item.DeletedAt));
            Assert.All(await assertionContext.RobotPrograms.IgnoreQueryFilters()
                    .Where(item => rootIds.Contains(item.Id)).ToArrayAsync(),
                item => Assert.NotNull(item.DeletedAt));
            Assert.All(await assertionContext.ConfigurationReleases.IgnoreQueryFilters()
                    .Where(item => rootIds.Contains(item.Id)).ToArrayAsync(),
                item => Assert.NotNull(item.DeletedAt));
        }
    }

    [IntegrationFact]
    public async Task Reconciliation_FailsOnlyStaleMaterializingUpgradeOnce()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        var now = DateTimeOffset.UtcNow;
        Guid upgradeId;

        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"stale-upgrade-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            var source = await setupContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(item => item.Id == installed.Data!.Id);
            var version = await setupContext.ProductionPackageVersions.AsNoTracking()
                .SingleAsync(item => item.Id == scenario.PackageVersionId);
            var upgrade = ProductionPackageUpgrade.Approve(
                scenario.OrganizationId, source.Id, version.Id,
                new string('a', 64), source.PackageManifestChecksum, version.ManifestChecksum!,
                [scenario.ProductSourceKey], $"stale-upgrade-{Guid.NewGuid():N}", actorId,
                now.AddHours(-1));
            setupContext.ProductionPackageUpgrades.Add(upgrade);
            await setupContext.SaveChangesAsync();
            upgradeId = upgrade.Id;
        }

        await using (var reconcileContext = _fixture.CreateDbContext())
        {
            var service = new ProductionPackageUpgradeReconciliationService(
                new ProductionPackageUpgradeStore(reconcileContext));
            var first = await service.ReconcileAsync(now, TimeSpan.FromMinutes(15), 100);
            var second = await service.ReconcileAsync(now.AddSeconds(1), TimeSpan.FromMinutes(15), 100);

            Assert.Equal(1, first.CandidateCount);
            Assert.Equal(1, first.FailedCount);
            Assert.Equal(0, second.FailedCount);
        }

        await using var assertionContext = _fixture.CreateDbContext();
        var failed = await assertionContext.ProductionPackageUpgrades.AsNoTracking()
            .SingleAsync(item => item.Id == upgradeId);
        Assert.Equal(ProductionPackageUpgradeStatus.Failed, failed.Status);
        Assert.Equal("UpgradeMaterializationTimedOut", failed.FailureCode);
    }

    [IntegrationFact]
    public async Task Preview_ReportsAddedAndRemovedProductsAcrossVersions()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid sourceInstallationId;
        Guid versionWithAddedProductId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var source = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"added-preview-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(source.Succeeded, source.Message);
            sourceInstallationId = source.Data!.Id;
            var target = await CreateVersionWithAddedProductAsync(
                setupContext, scenario.PackageVersionId, actorId, "MILKSHAKE");
            await setupContext.SaveChangesAsync();
            versionWithAddedProductId = target.Id;
        }

        await using (var addedPreviewContext = _fixture.CreateDbContext())
        {
            var preview = await CreateUpgradeService(addedPreviewContext, storage).PreviewAsync(
                user, scenario.OrganizationId, sourceInstallationId, versionWithAddedProductId,
                [scenario.ProductSourceKey, "MILKSHAKE"], CancellationToken.None);
            Assert.True(preview.Succeeded, preview.Message);
            Assert.Equal(["MILKSHAKE"], preview.Data!.AddedProductSourceKeys);
            Assert.Contains(preview.Data.Products,
                product => product.ProductSourceKey == "MILKSHAKE" && product.ChangeKind == "Added");
        }

        var removedActorId = Guid.NewGuid();
        var removedUser = new CurrentUserContext { AccountId = removedActorId, IsSystemAdmin = true };
        var removedScenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(
            _fixture, storage, removedActorId);
        Guid twoProductInstallationId;
        Guid removedTargetVersionId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var twoProductVersion = await CreateVersionWithAddedProductAsync(
                setupContext, removedScenario.PackageVersionId, removedActorId, "MILKSHAKE");
            await setupContext.SaveChangesAsync();
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = removedUser,
                    OrganizationId = removedScenario.OrganizationId,
                    PackageId = removedScenario.PackageId,
                    PackageVersionId = twoProductVersion.Id,
                    IdempotencyKey = $"removed-preview-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [removedScenario.ProductSourceKey, "MILKSHAKE"]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            twoProductInstallationId = installed.Data!.Id;
            var target = await CreateVersionWithoutProductAsync(
                setupContext, twoProductVersion.Id, removedActorId, "MILKSHAKE");
            await setupContext.SaveChangesAsync();
            removedTargetVersionId = target.Id;
        }

        await using var removedPreviewContext = _fixture.CreateDbContext();
        var removedPreview = await CreateUpgradeService(removedPreviewContext, storage).PreviewAsync(
            removedUser, removedScenario.OrganizationId, twoProductInstallationId, removedTargetVersionId,
            [], CancellationToken.None);
        Assert.True(removedPreview.Succeeded, removedPreview.Message);
        Assert.Equal(["MILKSHAKE"], removedPreview.Data!.RemovedProductSourceKeys);
        Assert.Contains(removedPreview.Data.Products,
            product => product.ProductSourceKey == "MILKSHAKE" && product.ChangeKind == "Removed");
    }

    [IntegrationFact]
    public async Task Preview_BlocksRequiredGroupWhenIncomingRemovesItsDefaultOption()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        Guid targetVersionId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var sourceVersion = await CreateVersionWithRequiredOptionsAsync(
                setupContext, scenario.PackageVersionId, actorId, removeDefault: false);
            await setupContext.SaveChangesAsync();
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = sourceVersion.Id,
                    IdempotencyKey = $"default-option-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            var target = await CreateVersionWithRequiredOptionsAsync(
                setupContext, sourceVersion.Id, actorId, removeDefault: true);
            await setupContext.SaveChangesAsync();
            targetVersionId = target.Id;
        }

        await using var previewContext = _fixture.CreateDbContext();
        var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
            user, scenario.OrganizationId, installationId, targetVersionId, [], CancellationToken.None);
        Assert.True(preview.Succeeded, preview.Message);
        Assert.Contains("DefaultOptionReplacementRequired:ICE_CREAM:FLAVOR", preview.Data!.Blockers);
    }

    [IntegrationFact]
    public async Task Preview_MaterializesSuccessorWhenPublishedArtifactContentChanges()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        var changedBytes = "return false"u8.ToArray();
        var changedChecksum = Convert.ToHexString(SHA256.HashData(changedBytes)).ToLowerInvariant();
        var changedStorageKey = $"robot-artifact-templates/{Guid.NewGuid():N}/base.lua";
        await storage.WriteImmutableAsync(
            new ArtifactObjectWriteRequest(
                changedStorageKey, "text/x-lua", changedBytes.LongLength, changedChecksum),
            new MemoryStream(changedBytes));

        Guid installationId;
        Guid targetVersionId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"artifact-change-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;

            var source = await LoadVersionDefinitionAsync(setupContext, scenario.PackageVersionId);
            var sourceArtifact = Assert.Single(source.Artifacts);
            var changedTemplate = RobotArtifactTemplate.CreateDraft(
                $"BASE-{Guid.NewGuid():N}", "Changed base", changedStorageKey, "base.lua",
                changedChecksum, "FAIRINO_LUA_V1", "FR5", changedBytes.LongLength,
                DateTimeOffset.UtcNow, technicalContractId: sourceArtifact.TechnicalContractId,
                technicalContractChecksum: sourceArtifact.TechnicalContractChecksum);
            changedTemplate.Publish();
            setupContext.RobotArtifactTemplates.Add(changedTemplate);

            var target = ProductionPackageVersion.CreateDraft(
                source.ProductionPackageId, source.Version + 1);
            target.ReplaceDefinition(
                source.Products.Select(CloneProductDefinition),
                [ProductionPackageArtifactDefinition.Create(
                    sourceArtifact.SourceKey, changedTemplate.Id, changedChecksum,
                    sourceArtifact.TechnicalContractId, sourceArtifact.TechnicalContractChecksum)],
                source.Programs.Select(program => ProductionPackageProgramBlueprint.Create(
                    program.BlueprintCode, program.RuntimeTargetCode, program.MachineModelCode,
                    program.Slots.Select(slot => (slot.SlotCode, slot.ArtifactSourceKey,
                        slot.RequiredEffectCode, slot.Phase, slot.IsRequired, slot.AllowMultiple, slot.SortHint)))),
                source.Routes.Select(CloneRoute));
            target.Publish(DateTimeOffset.UtcNow, actorId);
            setupContext.ProductionPackageVersions.Add(target);
            await setupContext.SaveChangesAsync();
            targetVersionId = target.Id;
        }

        await using var previewContext = _fixture.CreateDbContext();
        var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
            user, scenario.OrganizationId, installationId, targetVersionId,
            [scenario.ProductSourceKey], CancellationToken.None);

        Assert.True(preview.Succeeded, preview.Message);
        var artifact = Assert.Single(preview.Data!.Artifacts);
        Assert.Equal(changedChecksum, artifact.ArtifactChecksum);
        Assert.Equal("MaterializeSuccessorCopy", artifact.MaterializationAction);
    }

    [IntegrationFact]
    public async Task Preview_BlocksPackageManagedTechnicalDrift()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"drift-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            var product = await setupContext.Products.SingleAsync(
                value => value.OrganizationId == scenario.OrganizationId);
            product.ProductType = "UnauthorizedTechnicalChange";
            var target = await CloneAsNextPublishedVersionAsync(
                setupContext, scenario.PackageVersionId, actorId);
            await setupContext.SaveChangesAsync();
            scenario = scenario with { PackageVersionId = target.Id };
        }

        await using var previewContext = _fixture.CreateDbContext();
        var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
            user, scenario.OrganizationId, installationId, scenario.PackageVersionId, [], CancellationToken.None);

        Assert.True(preview.Succeeded, preview.Message);
        Assert.Contains("ManagedFieldDrift:ICE_CREAM", preview.Data!.Blockers);
        Assert.Equal("ReuseExistingCandidate", Assert.Single(preview.Data.Artifacts).MaterializationAction);
    }

    [IntegrationFact]
    public async Task Execute_RejectsApprovedPreviewWhenSourceScopeChangesBeforeMaterialization()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        Guid targetVersionId;
        string previewChecksum;

        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"scope-drift-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            targetVersionId = (await CloneAsNextPublishedVersionAsync(
                setupContext, scenario.PackageVersionId, actorId)).Id;
            await setupContext.SaveChangesAsync();
        }

        await using (var previewContext = _fixture.CreateDbContext())
        {
            var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
                user, scenario.OrganizationId, installationId, targetVersionId, [], CancellationToken.None);
            Assert.True(preview.Succeeded, preview.Message);
            Assert.Empty(preview.Data!.Blockers);
            previewChecksum = preview.Data.PreviewChecksum;
        }

        await using (var mutationContext = _fixture.CreateDbContext())
        {
            var product = await mutationContext.Products.SingleAsync(product =>
                product.OrganizationId == scenario.OrganizationId);
            product.Name = "Changed after approval preview";
            await mutationContext.SaveChangesAsync();
        }

        await using (var executeContext = _fixture.CreateDbContext())
        {
            var result = await CreateUpgradeService(executeContext, storage).ExecuteAsync(
                new ExecuteProductionPackageUpgradeCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    SourceInstallationId = installationId,
                    TargetPackageVersionId = targetVersionId,
                    PreviewChecksum = previewChecksum,
                    IdempotencyKey = $"scope-drift-upgrade-{Guid.NewGuid():N}"
                }, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("preview is stale", result.Message!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await executeContext.ProductionPackageUpgrades.CountAsync(
                value => value.SourceInstallationId == installationId));
        }
    }

    [IntegrationFact]
    public async Task ConcurrentExecute_AllowsOnlyOneActiveUpgradeForSourceInstallation()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"concurrent-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            var target = await CloneAsNextPublishedVersionAsync(
                setupContext, scenario.PackageVersionId, actorId);
            await setupContext.SaveChangesAsync();
            scenario = scenario with { PackageVersionId = target.Id };
        }

        string previewChecksum;
        await using (var previewContext = _fixture.CreateDbContext())
        {
            var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
                user, scenario.OrganizationId, installationId, scenario.PackageVersionId, [], CancellationToken.None);
            Assert.True(preview.Succeeded, preview.Message);
            Assert.Empty(preview.Data!.Blockers);
            previewChecksum = preview.Data.PreviewChecksum;
        }

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var first = CreateUpgradeService(firstContext, storage).ExecuteAsync(new ExecuteProductionPackageUpgradeCommand
        {
            UserContext = user,
            OrganizationId = scenario.OrganizationId,
            SourceInstallationId = installationId,
            TargetPackageVersionId = scenario.PackageVersionId,
            PreviewChecksum = previewChecksum,
            IdempotencyKey = $"concurrent-a-{Guid.NewGuid():N}"
        }, CancellationToken.None);
        var second = CreateUpgradeService(secondContext, storage).ExecuteAsync(new ExecuteProductionPackageUpgradeCommand
        {
            UserContext = user,
            OrganizationId = scenario.OrganizationId,
            SourceInstallationId = installationId,
            TargetPackageVersionId = scenario.PackageVersionId,
            PreviewChecksum = previewChecksum,
            IdempotencyKey = $"concurrent-b-{Guid.NewGuid():N}"
        }, CancellationToken.None);
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded && result.StatusCode == 409);
        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.ProductionPackageUpgrades.CountAsync(
            value => value.SourceInstallationId == installationId));
    }

    [IntegrationFact]
    public async Task ConcurrentExactRetry_ReturnsTheSamePreparedUpgrade()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };
        Guid installationId;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installed = await CreateInstallationService(setupContext, storage).InstallAsync(
                new InstallProductionPackageCommand
                {
                    UserContext = user,
                    OrganizationId = scenario.OrganizationId,
                    PackageId = scenario.PackageId,
                    PackageVersionId = scenario.PackageVersionId,
                    IdempotencyKey = $"exact-retry-source-{Guid.NewGuid():N}",
                    ProductSourceKeys = [scenario.ProductSourceKey]
                }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            installationId = installed.Data!.Id;
            var target = await CloneAsNextPublishedVersionAsync(
                setupContext, scenario.PackageVersionId, actorId);
            await setupContext.SaveChangesAsync();
            scenario = scenario with { PackageVersionId = target.Id };
        }

        string previewChecksum;
        await using (var previewContext = _fixture.CreateDbContext())
        {
            var preview = await CreateUpgradeService(previewContext, storage).PreviewAsync(
                user, scenario.OrganizationId, installationId, scenario.PackageVersionId, [], CancellationToken.None);
            Assert.True(preview.Succeeded, preview.Message);
            Assert.Empty(preview.Data!.Blockers);
            previewChecksum = preview.Data.PreviewChecksum;
        }

        var idempotencyKey = $"exact-retry-{Guid.NewGuid():N}";
        ExecuteProductionPackageUpgradeCommand CreateCommand() => new()
        {
            UserContext = user,
            OrganizationId = scenario.OrganizationId,
            SourceInstallationId = installationId,
            TargetPackageVersionId = scenario.PackageVersionId,
            PreviewChecksum = previewChecksum,
            IdempotencyKey = idempotencyKey
        };
        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var results = await Task.WhenAll(
            CreateUpgradeService(firstContext, storage).ExecuteAsync(CreateCommand(), CancellationToken.None),
            CreateUpgradeService(secondContext, storage).ExecuteAsync(CreateCommand(), CancellationToken.None));

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        var firstResult = Assert.IsType<ProductionPackageUpgradeResult>(results[0].Data);
        var secondResult = Assert.IsType<ProductionPackageUpgradeResult>(results[1].Data);
        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(firstResult.TargetInstallationId, secondResult.TargetInstallationId);
        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.ProductionPackageUpgrades.CountAsync(
            value => value.SourceInstallationId == installationId));
        Assert.Equal(1, await assertionContext.ProductionPackageInstallations.CountAsync(
            value => value.IdempotencyKey == $"production-package-upgrade:{firstResult.Id:N}"));
    }

    [IntegrationFact]
    public async Task UpgradePublishedPackage_CutoverAndRollback_PreservesCommercialStateAndMenuBinding()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(_fixture, storage, actorId);
        var user = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true };

        Guid sourceInstallationId;
        Guid sourceProductId;
        Guid sourceVariantId;
        Guid sourceRecipeId;
        Guid menuItemId;
        Guid lowCostEndpointId;
        string canonicalProductCode;
        await using (var setupContext = _fixture.CreateDbContext())
        {
            var installer = CreateInstallationService(setupContext, storage);
            var installed = await installer.InstallAsync(new InstallProductionPackageCommand
            {
                UserContext = user,
                OrganizationId = scenario.OrganizationId,
                PackageId = scenario.PackageId,
                PackageVersionId = scenario.PackageVersionId,
                IdempotencyKey = $"upgrade-source-{Guid.NewGuid():N}",
                ProductSourceKeys = [scenario.ProductSourceKey]
            }, CancellationToken.None);
            Assert.True(installed.Succeeded, installed.Message);
            sourceInstallationId = installed.Data!.Id;

            var sourceProduct = await setupContext.Products
                .Include(product => product.ProductVariants).ThenInclude(variant => variant.Recipes)
                .SingleAsync(product => product.OrganizationId == scenario.OrganizationId);
            sourceProduct.Name = "Franchise display name";
            sourceProduct.BasePrice = 42_000;
            sourceProduct.IsAvailable = true;
            var sourceVariant = Assert.Single(sourceProduct.ProductVariants);
            sourceVariant.Name = "Franchise variant name";
            sourceVariant.BasePrice = 43_000;
            sourceVariant.IsAvailable = true;
            var sourceRecipe = Assert.Single(sourceVariant.Recipes);
            canonicalProductCode = sourceProduct.Code;
            sourceProductId = sourceProduct.Id;
            sourceVariantId = sourceVariant.Id;
            sourceRecipeId = sourceRecipe.Id;

            var menu = new Menu
            {
                OrganizationId = scenario.OrganizationId,
                Code = $"MENU-{Guid.NewGuid():N}",
                Name = "Upgrade test menu",
                Status = MenuStatus.Draft,
                Currency = "VND"
            };
            var menuItem = new MenuItem
            {
                MenuId = menu.Id,
                ProductId = sourceProduct.Id,
                ProductVariantId = sourceVariant.Id,
                RecipeId = sourceRecipe.Id,
                Code = $"ITEM-{Guid.NewGuid():N}",
                DisplayName = "Franchise menu item",
                Status = MenuItemStatus.Active,
                Price = 45_000,
                Currency = "VND"
            };
            menu.MenuItems.Add(menuItem);
            setupContext.Menus.Add(menu);
            menuItemId = menuItem.Id;

            var lowCostEndpoint = KioskExecutionEndpoint.CreateProvisioning(
                scenario.KioskId, $"LOW-{Guid.NewGuid():N}", KioskExecutionProfile.LowCostController,
                ExecutionEndpointAuthenticationMode.SignedCommandTls);
            setupContext.KioskExecutionEndpoints.Add(lowCostEndpoint);
            await setupContext.SaveChangesAsync();
            var lowCostCredential = lowCostEndpoint.ProvisionCredential(
                $"key-{Guid.NewGuid():N}", DateTimeOffset.UtcNow, "test-public-key");
            lowCostEndpoint.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
            setupContext.ExecutionEndpointCredentialBindings.Add(lowCostCredential);
            lowCostEndpointId = lowCostEndpoint.Id;

            var targetVersion = await CloneAsNextPublishedVersionAsync(setupContext, scenario.PackageVersionId, actorId);
            await setupContext.SaveChangesAsync();
            scenario = scenario with { PackageVersionId = targetVersion.Id };
        }

        PublishedInstallation sourcePublished;
        await using (var sourcePublishContext = _fixture.CreateDbContext())
        {
            sourcePublished = await PublishInstallationReleaseAsync(
                sourcePublishContext, sourceInstallationId, actorId);
        }
        var sourceDeploymentId = Guid.NewGuid();
        var sourceLowCostDeploymentId = Guid.NewGuid();
        await ActivateFullEdgeAsync(_fixture, scenario.ExecutionEndpointId, sourceDeploymentId,
            sourcePublished.ReleaseId, sourcePublished.ReleaseChecksum);
        await ActivateLowCostAsync(_fixture, lowCostEndpointId, sourceLowCostDeploymentId,
            sourcePublished.ReleaseId, sourcePublished.ReleaseChecksum);

        Guid upgradeId;
        Guid targetInstallationId;
        ExecuteProductionPackageUpgradeCommand command;
        var rollbackDispatcher = new RecordingRollbackDispatcher();
        await using (var upgradeContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(upgradeContext, storage, rollbackDispatcher);
            var preview = await service.PreviewAsync(user, scenario.OrganizationId, sourceInstallationId,
                scenario.PackageVersionId, [], CancellationToken.None);
            Assert.True(preview.Succeeded, preview.Message);
            Assert.True(preview.Data!.Blockers.Count == 0,
                $"Unexpected blockers: {JsonSerializer.Serialize(preview.Data.Blockers)}; " +
                await DescribeManagedProductAsync(upgradeContext, sourceInstallationId));
            Assert.Equal(1, preview.Data.AffectedMenuItemCount);
            Assert.Equal(2, preview.Data.RequiredEndpointCount);
            Assert.Single(preview.Data.Products);
            Assert.Single(preview.Data.MenuChanges);
            Assert.Single(preview.Data.Artifacts);
            Assert.Equal("ReuseExistingCandidate", preview.Data.Artifacts.Single().MaterializationAction);
            Assert.Equal(2, preview.Data.Endpoints.Count);

            command = new ExecuteProductionPackageUpgradeCommand
            {
                UserContext = user,
                OrganizationId = scenario.OrganizationId,
                SourceInstallationId = sourceInstallationId,
                TargetPackageVersionId = scenario.PackageVersionId,
                PreviewChecksum = preview.Data.PreviewChecksum,
                IdempotencyKey = $"upgrade-{Guid.NewGuid():N}"
            };
            var stale = await service.ExecuteAsync(new ExecuteProductionPackageUpgradeCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                SourceInstallationId = command.SourceInstallationId,
                TargetPackageVersionId = command.TargetPackageVersionId,
                PreviewChecksum = new string('0', 64),
                IdempotencyKey = $"stale-{Guid.NewGuid():N}"
            }, CancellationToken.None);
            Assert.False(stale.Succeeded);
            Assert.Equal(409, stale.StatusCode);
            var executed = await service.ExecuteAsync(command, CancellationToken.None);
            Assert.True(executed.Succeeded, executed.Message);
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.ReadyForReview), executed.Data!.Status);
            upgradeId = executed.Data.Id;
            targetInstallationId = executed.Data.TargetInstallationId!.Value;

            await upgradeContext.ProductionPackageUpgradeMenuOptionChanges
                .Where(value => value.UpgradeMenuChange.UpgradeId == upgradeId).ExecuteDeleteAsync();
            await upgradeContext.ProductionPackageUpgradeMenuChanges
                .Where(value => value.UpgradeId == upgradeId).ExecuteDeleteAsync();
            await upgradeContext.ProductionPackageUpgradeEndpointTargets
                .Where(value => value.UpgradeId == upgradeId).ExecuteDeleteAsync();
            await upgradeContext.ProductionPackageUpgradeCatalogIdentityChanges
                .Where(value => value.UpgradeId == upgradeId).ExecuteDeleteAsync();
            await upgradeContext.ProductionPackageUpgradeAvailabilityChanges
                .Where(value => value.UpgradeId == upgradeId).ExecuteDeleteAsync();
            await upgradeContext.ProductionPackageUpgrades.Where(value => value.Id == upgradeId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(value => value.Status, ProductionPackageUpgradeStatus.Failed)
                    .SetProperty(value => value.FailureCode, "PREPARATION_FAILED")
                    .SetProperty(value => value.FailureMessage, "Injected preparation failure."));
            upgradeContext.ChangeTracker.Clear();

            var retry = await service.ExecuteAsync(command, CancellationToken.None);
            Assert.True(retry.Succeeded, retry.Message);
            Assert.Equal(upgradeId, retry.Data!.Id);
            Assert.Equal(targetInstallationId, retry.Data.TargetInstallationId);
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.ReadyForReview), retry.Data.Status);
            var history = await service.ListAsync(user, scenario.OrganizationId, sourceInstallationId,
                null, 1, 20, CancellationToken.None);
            Assert.True(history.Succeeded, history.Message);
            Assert.Single(history.Data!);
        }

        PublishedInstallation targetPublished;
        await using (var publishContext = _fixture.CreateDbContext())
        {
            targetPublished = await PublishInstallationReleaseAsync(
                publishContext, targetInstallationId, actorId);
        }
        var targetDeploymentId = Guid.NewGuid();
        var targetLowCostDeploymentId = Guid.NewGuid();
        await ActivateFullEdgeAsync(_fixture, scenario.ExecutionEndpointId, targetDeploymentId,
            targetPublished.ReleaseId, targetPublished.ReleaseChecksum);
        await ActivateLowCostAsync(_fixture, lowCostEndpointId, targetLowCostDeploymentId,
            targetPublished.ReleaseId, targetPublished.ReleaseChecksum);

        await using (var invalidEvidenceContext = _fixture.CreateDbContext())
        {
            await invalidEvidenceContext.ControllerArtifactSetDeployments
                .Where(value => value.Id == targetLowCostDeploymentId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    value => value.Status,
                    Domain.ProductionConfiguration.Enums.ControllerArtifactSetDeploymentStatus.Failed));
            var service = CreateUpgradeService(invalidEvidenceContext, storage, rollbackDispatcher);
            var blocked = await service.CutoverAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, CancellationToken.None);
            Assert.False(blocked.Succeeded);
            Assert.Equal(409, blocked.StatusCode);
            await invalidEvidenceContext.ControllerArtifactSetDeployments
                .Where(value => value.Id == targetLowCostDeploymentId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    value => value.Status,
                    Domain.ProductionConfiguration.Enums.ControllerArtifactSetDeploymentStatus.Active));
        }

        await using (var cutoverContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(cutoverContext, storage, rollbackDispatcher);
            var cutover = await service.CutoverAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, CancellationToken.None);
            Assert.True(cutover.Succeeded, cutover.Message);
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.Completed), cutover.Data!.Status);
        }

        await using (var cutoverAssertionContext = _fixture.CreateDbContext())
        {
            var source = await cutoverAssertionContext.Products.AsNoTracking().SingleAsync(x => x.Id == sourceProductId);
            var target = await cutoverAssertionContext.Products.AsNoTracking().SingleAsync(x => x.Id == targetPublished.ProductId);
            var menuItem = await cutoverAssertionContext.MenuItems.AsNoTracking().SingleAsync(x => x.Id == menuItemId);
            Assert.NotEqual(canonicalProductCode, source.Code);
            Assert.Equal(canonicalProductCode, target.Code);
            Assert.Equal("Franchise display name", target.Name);
            Assert.Equal(42_000, target.BasePrice);
            Assert.Equal(targetPublished.ProductId, menuItem.ProductId);
            Assert.Equal(targetPublished.VariantId, menuItem.ProductVariantId);
            Assert.Equal(targetPublished.RecipeId, menuItem.RecipeId);
        }
        await using (var postCutoverEditContext = _fixture.CreateDbContext())
        {
            var menuItem = await postCutoverEditContext.MenuItems.SingleAsync(x => x.Id == menuItemId);
            menuItem.Price = 46_000;
            menuItem.RecipeId = null;
            await postCutoverEditContext.SaveChangesAsync();
        }

        await using (var firstRollbackContext = _fixture.CreateDbContext())
        await using (var secondRollbackContext = _fixture.CreateDbContext())
        {
            var rollbacks = await Task.WhenAll(
                CreateUpgradeService(firstRollbackContext, storage, rollbackDispatcher).RollbackAsync(
                    user, scenario.OrganizationId, sourceInstallationId, upgradeId, null,
                    "Integration rollback", CancellationToken.None),
                CreateUpgradeService(secondRollbackContext, storage, rollbackDispatcher).RollbackAsync(
                    user, scenario.OrganizationId, sourceInstallationId, upgradeId, null,
                    "Integration rollback", CancellationToken.None));

            Assert.All(rollbacks, rollback =>
            {
                Assert.True(rollback.Succeeded, rollback.Message);
                Assert.Equal(202, rollback.StatusCode);
                Assert.Equal(nameof(ProductionPackageUpgradeStatus.RollbackPending), rollback.Data!.Status);
            });
            Assert.Equal(2, rollbackDispatcher.Commands.Count);
        }

        rollbackDispatcher.FailAllPending("ExecutionReportTimeout", "Controller did not report activation.");
        await using (var rollbackRetryContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(rollbackRetryContext, storage, rollbackDispatcher);
            var retry = await service.RollbackAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, null, "Retry failed rollback deployment", CancellationToken.None);
            Assert.True(retry.Succeeded, retry.Message);
            Assert.Equal(202, retry.StatusCode);
            Assert.Equal(4, rollbackDispatcher.Commands.Count);

            var detail = await service.GetAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, CancellationToken.None);
            Assert.True(detail.Succeeded, detail.Message);
            Assert.Equal(2, detail.Data!.Endpoints.Count);
            Assert.All(detail.Data.Endpoints, endpoint =>
            {
                Assert.Equal(2, endpoint.RollbackAttempts.Count);
                Assert.Equal("Failed", endpoint.RollbackAttempts.First().DeploymentStatus);
                Assert.Equal("Pending", endpoint.RollbackAttempts.Last().DeploymentStatus);
                Assert.Equal("Retry failed rollback deployment", endpoint.RollbackAttempts.Last().Reason);
            });
        }

        await ActivateFullEdgeAsync(_fixture, scenario.ExecutionEndpointId,
            rollbackDispatcher.NewDeploymentIds[sourceDeploymentId],
            sourcePublished.ReleaseId, sourcePublished.ReleaseChecksum);
        await ActivateLowCostAsync(_fixture, lowCostEndpointId,
            rollbackDispatcher.NewDeploymentIds[sourceLowCostDeploymentId],
            sourcePublished.ReleaseId, sourcePublished.ReleaseChecksum);
        rollbackDispatcher.ActivateAllPending();
        await using (var conflictContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(conflictContext, storage, rollbackDispatcher);
            var conflict = await service.RollbackAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, null, "Integration rollback", CancellationToken.None);
            Assert.False(conflict.Succeeded);
            Assert.Equal(409, conflict.StatusCode);
        }
        await using (var resolveConflictContext = _fixture.CreateDbContext())
        {
            var menuItem = await resolveConflictContext.MenuItems.SingleAsync(x => x.Id == menuItemId);
            menuItem.RecipeId = targetPublished.RecipeId;
            await resolveConflictContext.SaveChangesAsync();
        }
        await using (var rollbackCompletionContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(rollbackCompletionContext, storage, rollbackDispatcher);
            var rollback = await service.RollbackAsync(user, scenario.OrganizationId, sourceInstallationId,
                upgradeId, null, "Integration rollback", CancellationToken.None);
            Assert.True(rollback.Succeeded, rollback.Message);
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.RolledBack), rollback.Data!.Status);
        }

        await using (var finalContext = _fixture.CreateDbContext())
        {
            var source = await finalContext.Products.AsNoTracking().SingleAsync(x => x.Id == sourceProductId);
            var target = await finalContext.Products.AsNoTracking().SingleAsync(x => x.Id == targetPublished.ProductId);
            var sourceInstallation = await finalContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(x => x.Id == sourceInstallationId);
            var targetInstallation = await finalContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(x => x.Id == targetInstallationId);
            var menuItem = await finalContext.MenuItems.AsNoTracking().SingleAsync(x => x.Id == menuItemId);
            Assert.Equal(canonicalProductCode, source.Code);
            Assert.NotEqual(canonicalProductCode, target.Code);
            Assert.Equal(ProductionPackageInstallationStatus.Installed, sourceInstallation.Status);
            Assert.Equal(ProductionPackageInstallationStatus.Superseded, targetInstallation.Status);
            Assert.Equal(sourceProductId, menuItem.ProductId);
            Assert.Equal(sourceVariantId, menuItem.ProductVariantId);
            Assert.Equal(sourceRecipeId, menuItem.RecipeId);
            Assert.Equal(46_000, menuItem.Price);
            Assert.Equal(1, await finalContext.ProductionPackageUpgrades.CountAsync(x => x.Id == upgradeId));
        }
        await using (var terminalRetryContext = _fixture.CreateDbContext())
        {
            var service = CreateUpgradeService(terminalRetryContext, storage, rollbackDispatcher);
            var retry = await service.ExecuteAsync(command, CancellationToken.None);
            Assert.True(retry.Succeeded, retry.Message);
            Assert.Equal(nameof(ProductionPackageUpgradeStatus.RolledBack), retry.Data!.Status);
            Assert.Equal(upgradeId, retry.Data.Id);
        }
    }

    private static async Task<ProductionPackageVersion> CloneAsNextPublishedVersionAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid sourceVersionId, Guid actorId)
    {
        var source = await dbContext.ProductionPackageVersions.AsNoTracking()
            .Include(version => version.Products)
            .Include(version => version.Artifacts)
            .Include(version => version.Programs).ThenInclude(program => program.Slots)
            .Include(version => version.Routes)
            .SingleAsync(version => version.Id == sourceVersionId);
        var target = ProductionPackageVersion.CreateDraft(source.ProductionPackageId, source.Version + 1);
        target.ReplaceDefinition(
            source.Products.Select(product => ProductionPackageProductDefinition.Create(
                product.SourceKey, product.SourceProductId, product.ProductSnapshotJson)),
            source.Artifacts.Select(artifact => ProductionPackageArtifactDefinition.Create(
                artifact.SourceKey, artifact.RobotArtifactTemplateId, artifact.ArtifactChecksum,
                artifact.TechnicalContractId, artifact.TechnicalContractChecksum)),
            source.Programs.Select(program => ProductionPackageProgramBlueprint.Create(
                program.BlueprintCode, program.RuntimeTargetCode, program.MachineModelCode,
                program.Slots.Select(slot => (slot.SlotCode, slot.ArtifactSourceKey,
                    slot.RequiredEffectCode, slot.Phase, slot.IsRequired, slot.AllowMultiple, slot.SortHint)))),
            source.Routes.Select(route => ProductionPackageRouteBlueprint.Create(
                route.RouteCode, route.ProductSourceKey, route.ProductVariantSourceKey,
                route.RecipeSourceKey, route.GetSupportedOptionCodes(), route.ProgramBlueprintCode,
                route.RequiredCapabilitiesJson, route.Priority)));
        target.Publish(DateTimeOffset.UtcNow, actorId);
        dbContext.ProductionPackageVersions.Add(target);
        return target;
    }

    private static async Task<ProductionPackageVersion> CreateVersionWithAddedProductAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid sourceVersionId, Guid actorId,
        string addedSourceKey)
    {
        var source = await LoadVersionDefinitionAsync(dbContext, sourceVersionId);
        var sourceProduct = Assert.Single(source.Products);
        var addedSnapshot = CloneProductSnapshot(sourceProduct.ProductSnapshotJson, addedSourceKey);
        var addedDocument = JsonNode.Parse(addedSnapshot)!["Product"]!;
        var addedSourceProductId = addedDocument["Id"]!.GetValue<Guid>();
        var addedSourceVariantId = addedDocument["Variants"]![0]!["Id"]!.GetValue<Guid>();
        var addedSourceRecipeId = addedDocument["Variants"]![0]!["Recipes"]![0]!["Id"]!.GetValue<Guid>();
        var variantCode = addedDocument["Variants"]![0]!["Code"]!.GetValue<string>();
        var recipeCode = addedDocument["Variants"]![0]!["Recipes"]![0]!["Code"]!.GetValue<string>();
        var addedSourceCatalogProduct = new Domain.Catalog.Entities.Product
        {
            Id = addedSourceProductId,
            Code = $"SOURCE-{Guid.NewGuid():N}",
            Name = $"Source {addedSourceKey}",
            ProductType = "IceCream",
            Currency = "VND",
            IsAvailable = true
        };
        var addedSourceVariant = new Domain.Catalog.Entities.ProductVariant
        {
            Id = addedSourceVariantId,
            ProductId = addedSourceProductId,
            Code = variantCode,
            Name = variantCode,
            FulfillmentType = Domain.Catalog.Enums.FulfillmentType.MachineProduced,
            Currency = "VND",
            IsAvailable = true
        };
        addedSourceVariant.Recipes.Add(new Domain.Catalog.Entities.Recipe
        {
            Id = addedSourceRecipeId,
            ProductVariantId = addedSourceVariantId,
            Code = recipeCode,
            Name = recipeCode,
            Version = 1,
            IsDefault = true
        });
        addedSourceCatalogProduct.ProductVariants.Add(addedSourceVariant);
        dbContext.Products.Add(addedSourceCatalogProduct);
        var products = source.Products.Select(CloneProductDefinition).Append(
            ProductionPackageProductDefinition.Create(
                addedSourceKey, addedSourceProductId, addedSnapshot));
        var routes = source.Routes.Select(CloneRoute).Append(ProductionPackageRouteBlueprint.Create(
            $"{addedSourceKey}_ROUTE", addedSourceKey, variantCode, recipeCode, [],
            source.Programs.Single().BlueprintCode,
            source.Routes.Single().RequiredCapabilitiesJson,
            source.Routes.Max(route => route.Priority) + 1));
        return AddPublishedVersion(dbContext, source, actorId, products, routes);
    }

    private static async Task<ProductionPackageVersion> CreateVersionWithoutProductAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid sourceVersionId, Guid actorId,
        string removedSourceKey)
    {
        var source = await LoadVersionDefinitionAsync(dbContext, sourceVersionId);
        return AddPublishedVersion(dbContext, source, actorId,
            source.Products.Where(product => product.SourceKey != removedSourceKey).Select(CloneProductDefinition),
            source.Routes.Where(route => route.ProductSourceKey != removedSourceKey).Select(CloneRoute));
    }

    private static async Task<ProductionPackageVersion> CreateVersionWithRequiredOptionsAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid sourceVersionId, Guid actorId,
        bool removeDefault)
    {
        var source = await LoadVersionDefinitionAsync(dbContext, sourceVersionId);
        var definition = Assert.Single(source.Products);
        var document = JsonNode.Parse(definition.ProductSnapshotJson)!;
        var product = document["Product"]!;
        var options = new JsonArray
        {
            new JsonObject
            {
                ["Id"] = Guid.NewGuid(), ["Code"] = "CHOCOLATE", ["Name"] = "Chocolate",
                ["PriceDelta"] = 0, ["ExecutionImpact"] = 0, ["IsDefault"] = false,
                ["DisplayOrder"] = 2, ["IngredientRequirements"] = new JsonArray()
            }
        };
        if (!removeDefault)
            options.Insert(0, new JsonObject
            {
                ["Id"] = Guid.NewGuid(), ["Code"] = "VANILLA", ["Name"] = "Vanilla",
                ["PriceDelta"] = 0, ["ExecutionImpact"] = 0, ["IsDefault"] = true,
                ["DisplayOrder"] = 1, ["IngredientRequirements"] = new JsonArray()
            });
        product["OptionGroups"] = new JsonArray(new JsonObject
        {
            ["Id"] = removeDefault ? 902 : 901,
            ["Code"] = "FLAVOR", ["Name"] = "Flavor", ["SelectionType"] = 0,
            ["MinSelections"] = 1, ["MaxSelections"] = 1, ["IsRequired"] = true,
            ["IsActive"] = true, ["DisplayOrder"] = 1, ["Options"] = options
        });
        var products = new[]
        {
            ProductionPackageProductDefinition.Create(
                definition.SourceKey, definition.SourceProductId, document.ToJsonString())
        };
        return AddPublishedVersion(dbContext, source, actorId, products, source.Routes.Select(CloneRoute));
    }

    private static async Task<ProductionPackageVersion> LoadVersionDefinitionAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid sourceVersionId) =>
        await dbContext.ProductionPackageVersions.AsNoTracking()
            .Include(version => version.Products)
            .Include(version => version.Artifacts)
            .Include(version => version.Programs).ThenInclude(program => program.Slots)
            .Include(version => version.Routes)
            .SingleAsync(version => version.Id == sourceVersionId);

    private static ProductionPackageVersion AddPublishedVersion(
        global::Infrastructure.Data.IceBotDbContext dbContext, ProductionPackageVersion source, Guid actorId,
        IEnumerable<ProductionPackageProductDefinition> products,
        IEnumerable<ProductionPackageRouteBlueprint> routes)
    {
        var target = ProductionPackageVersion.CreateDraft(source.ProductionPackageId, source.Version + 1);
        target.ReplaceDefinition(products,
            source.Artifacts.Select(artifact => ProductionPackageArtifactDefinition.Create(
                artifact.SourceKey, artifact.RobotArtifactTemplateId, artifact.ArtifactChecksum,
                artifact.TechnicalContractId, artifact.TechnicalContractChecksum)),
            source.Programs.Select(program => ProductionPackageProgramBlueprint.Create(
                program.BlueprintCode, program.RuntimeTargetCode, program.MachineModelCode,
                program.Slots.Select(slot => (slot.SlotCode, slot.ArtifactSourceKey,
                    slot.RequiredEffectCode, slot.Phase, slot.IsRequired, slot.AllowMultiple, slot.SortHint)))),
            routes);
        target.Publish(DateTimeOffset.UtcNow, actorId);
        dbContext.ProductionPackageVersions.Add(target);
        return target;
    }

    private static ProductionPackageProductDefinition CloneProductDefinition(
        ProductionPackageProductDefinition product) => ProductionPackageProductDefinition.Create(
        product.SourceKey, product.SourceProductId, product.ProductSnapshotJson);

    private static ProductionPackageRouteBlueprint CloneRoute(ProductionPackageRouteBlueprint route) =>
        ProductionPackageRouteBlueprint.Create(route.RouteCode, route.ProductSourceKey,
            route.ProductVariantSourceKey, route.RecipeSourceKey, route.GetSupportedOptionCodes(),
            route.ProgramBlueprintCode, route.RequiredCapabilitiesJson, route.Priority);

    private static string CloneProductSnapshot(string sourceJson, string sourceKey)
    {
        var document = JsonNode.Parse(sourceJson)!;
        var product = document["Product"]!;
        product["Id"] = Guid.NewGuid();
        product["Code"] = sourceKey;
        product["Name"] = sourceKey;
        var variant = product["Variants"]![0]!;
        variant["Id"] = Guid.NewGuid();
        variant["Code"] = $"{sourceKey}_STANDARD";
        var recipe = variant["Recipes"]![0]!;
        recipe["Id"] = Guid.NewGuid();
        recipe["Code"] = $"{sourceKey}_DEFAULT";
        foreach (var item in recipe["Items"]!.AsArray()) item!["Id"] = Guid.NewGuid();
        return document.ToJsonString();
    }

    private static async Task<string> DescribeManagedProductAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid installationId)
    {
        var installation = await dbContext.ProductionPackageInstallations.AsNoTracking()
            .Include(value => value.PackageVersion).ThenInclude(value => value.Products)
            .SingleAsync(value => value.Id == installationId);
        var productId = Guid.Parse((await dbContext.ProductionPackageMaterializations.AsNoTracking().SingleAsync(
            value => value.InstallationId == installationId &&
                     value.ResourceKind == ProductionPackageResourceKind.Product)).TargetKey);
        var product = await dbContext.Products.AsNoTracking()
            .Include(value => value.ProductVariants).ThenInclude(value => value.Recipes).ThenInclude(value => value.RecipeItems)
            .SingleAsync(value => value.Id == productId);
        return JsonSerializer.Serialize(new
        {
            Snapshot = installation.PackageVersion.Products.Single().ProductSnapshotJson,
            Actual = new
            {
                product.Code,
                product.ProductType,
                product.PreparationTimeSeconds,
                Variants = product.ProductVariants.Select(variant => new
                {
                    variant.Code,
                    variant.VariantType,
                    variant.FulfillmentType,
                    variant.SizeCode,
                    variant.PreparationTimeSeconds,
                    Recipes = variant.Recipes.Select(recipe => new
                    {
                        recipe.Code,
                        recipe.IsDefault,
                        recipe.YieldQuantity,
                        recipe.Unit,
                        recipe.EstimatedDurationSeconds,
                        recipe.EffectiveFrom,
                        recipe.EffectiveTo,
                        recipe.InstructionsSchemaVersion,
                        recipe.InstructionsJson,
                        Items = recipe.RecipeItems.Select(item => new
                        {
                            item.IngredientId,
                            item.Quantity,
                            item.Unit,
                            item.StepOrder,
                            item.IsOptional,
                            item.Notes
                        })
                    })
                })
            }
        });
    }

    private static async Task<PublishedInstallation> PublishInstallationReleaseAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext, Guid installationId, Guid actorId)
    {
        var installation = await dbContext.ProductionPackageInstallations.AsNoTracking()
            .SingleAsync(value => value.Id == installationId);
        var productId = Guid.Parse((await dbContext.ProductionPackageMaterializations.AsNoTracking().SingleAsync(
            value => value.InstallationId == installationId &&
                     value.ResourceKind == ProductionPackageResourceKind.Product)).TargetKey);
        var product = await dbContext.Products
            .Include(value => value.ProductVariants).ThenInclude(value => value.Recipes).ThenInclude(value => value.RecipeItems)
            .SingleAsync(value => value.Id == productId);
        var variant = Assert.Single(product.ProductVariants);
        var recipe = Assert.Single(variant.Recipes);
        recipe.Publish(actorId, DateTimeOffset.UtcNow);

        var artifactId = Guid.Parse((await dbContext.ProductionPackageMaterializations.AsNoTracking().SingleAsync(
            value => value.InstallationId == installationId &&
                     value.ResourceKind == ProductionPackageResourceKind.RobotArtifact)).TargetKey);
        var artifact = await dbContext.RobotArtifacts.SingleAsync(value => value.Id == artifactId);
        if (artifact.Status == Domain.RobotConfiguration.Artifacts.RobotArtifactStatus.Draft)
            artifact.Publish();

        var programId = Guid.Parse((await dbContext.ProductionPackageMaterializations.AsNoTracking().SingleAsync(
            value => value.InstallationId == installationId &&
                     value.ResourceKind == ProductionPackageResourceKind.RobotProgram)).TargetKey);
        var program = await dbContext.RobotPrograms.Include(value => value.RobotProgramArtifacts)
            .SingleAsync(value => value.Id == programId);
        var artifactSnapshots = program.RobotProgramArtifacts.Select(link => new RobotArtifactManifestSnapshot(
            artifact.Id, artifact.ArtifactCode, artifact.ArtifactName, artifact.FileName, artifact.Status,
            artifact.Checksum, artifact.StorageKey, artifact.RuntimeTargetCode, artifact.MachineModelCode,
            artifact.ContentLengthBytes, artifact.TechnicalContractId, artifact.TechnicalContractChecksum)).ToArray();
        program.Publish(DateTimeOffset.UtcNow, artifactSnapshots);

        var release = await dbContext.ConfigurationReleases
            .Include(value => value.ExecutionRoutes).ThenInclude(value => value.RobotBindings)
            .SingleAsync(value => value.Id == installation.DraftConfigurationReleaseId);
        var publishedArtifacts = program.RobotProgramArtifacts.Select(link => new PublishedRobotArtifactSnapshot(
            link.Id, link.RobotArtifactId, link.RunOrder, link.ParametersSchemaVersion, link.ParametersJson,
            artifact.Checksum, artifact.StorageKey, artifact.RuntimeTargetCode, artifact.MachineModelCode,
            artifact.ContentLengthBytes, artifact.TechnicalContractId, artifact.TechnicalContractChecksum,
            link.RequiredOptionCode)).ToArray();
        release.Publish(DateTimeOffset.UtcNow, actorId, new Dictionary<Guid, PublishedRobotProgramSnapshot>
        {
            [program.Id] = new(program.Id, program.Code, program.OrganizationId!.Value,
                program.ProgramManifestSchemaVersion, program.ProgramManifestChecksum!, publishedArtifacts)
        });
        await dbContext.SaveChangesAsync();
        return new PublishedInstallation(
            product.Id, variant.Id, recipe.Id, release.Id, release.ReleaseChecksum!);
    }

    private static async Task ActivateFullEdgeAsync(IntegrationTestFixture fixture, Guid endpointId,
        Guid deploymentId, Guid releaseId, string releaseChecksum)
    {
        await using var dbContext = fixture.CreateDbContext();
        var endpoint = await dbContext.KioskExecutionEndpoints.Include(value => value.Kiosk)
            .SingleAsync(value => value.Id == endpointId);
        var deployment = KioskConfigurationDeployment.CreatePending(
            endpoint.KioskId,
            endpoint.Kiosk.OrganizationId,
            endpoint.Id,
            endpoint.FullEdgeRuntimeId!.Value,
            releaseId,
            releaseChecksum,
            1,
            $"upgrade-test-{deploymentId:N}",
            DateTimeOffset.UtcNow,
            null,
            "validation-checksum",
            "UnprovenPhysicalBehavior",
            "[]");
        deployment.Id = deploymentId;
        deployment.MarkInstalled(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        deployment.MarkActive(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        dbContext.KioskConfigurationDeployments.Add(deployment);
        endpoint.ApplyFullEdgeObservedActivation(deploymentId, releaseId, releaseChecksum,
            Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    private static async Task ActivateLowCostAsync(IntegrationTestFixture fixture, Guid endpointId,
        Guid deploymentId, Guid releaseId, string releaseChecksum)
    {
        await using var dbContext = fixture.CreateDbContext();
        var endpoint = await dbContext.KioskExecutionEndpoints.Include(value => value.Kiosk)
            .SingleAsync(value => value.Id == endpointId);
        var deployment = ControllerArtifactSetDeployment.CreatePending(
            endpoint.KioskId,
            endpoint.Kiosk.OrganizationId,
            endpoint.Id,
            endpoint.ControllerId!.Value,
            releaseId,
            releaseChecksum,
            1,
            $"upgrade-test-{deploymentId:N}",
            10,
            1024 * 1024,
            null,
            DateTimeOffset.UtcNow,
            [new ControllerArtifactSetItemSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), new string('p', 64), Guid.NewGuid(), new string('a', 64),
                "robot-artifacts/upgrade-test.lua", "FAIRINO_LUA_V1", "FR5", null,
                128, 1, 1, null)],
            "validation-checksum",
            "UnprovenPhysicalBehavior",
            "[]");
        deployment.Id = deploymentId;
        deployment.MarkInstalled(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        deployment.MarkActive(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        dbContext.ControllerArtifactSetDeployments.Add(deployment);
        endpoint.ApplyLowCostObservedActivation(deploymentId, releaseId, releaseChecksum, 1,
            deployment.ActiveSetChecksum, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    private static ProductionPackageUpgradeService CreateUpgradeService(
        global::Infrastructure.Data.IceBotDbContext dbContext, IArtifactObjectStorage storage,
        IConfigurationDeploymentRollbackDispatcher? rollbackDispatcher = null)
    {
        var dispatcher = rollbackDispatcher ?? new RecordingRollbackDispatcher();
        var packageStore = new ProductionPackageStore(dbContext);
        var upgradeStore = new ProductionPackageUpgradeStore(dbContext);
        return new ProductionPackageUpgradeService(
            upgradeStore,
            CreateInstallationService(dbContext, storage),
            new ProductionPackageUpgradePreviewService(packageStore, upgradeStore),
            new PostgresTechnicalResourceMutationCoordinator(dbContext),
            new ProductionPackageUpgradeMutationPolicy(new ProductionPackageUpgradeStore(dbContext)),
            dispatcher,
            dispatcher as IConfigurationDeploymentObservationReader ?? new EmptyDeploymentObservationReader());
    }

    private static ProductionPackageInstallationService CreateInstallationService(
        global::Infrastructure.Data.IceBotDbContext dbContext, IArtifactObjectStorage storage)
    {
        var contentService = new ArtifactUploadContentService(
            storage, NullLogger<ArtifactUploadContentService>.Instance);
        return new ProductionPackageInstallationService(
            new ProductionPackageStore(dbContext),
            new ProductionPackageInstallationStore(dbContext),
            storage,
            contentService,
            new ArtifactPublicationValidator(new RobotArtifactTechnicalContractStore(dbContext), storage),
            new PostgresTechnicalResourceMutationCoordinator(dbContext));
    }

    private sealed record PublishedInstallation(
        Guid ProductId,
        Guid VariantId,
        Guid RecipeId,
        Guid ReleaseId,
        string ReleaseChecksum);

    private sealed class RecordingRollbackDispatcher : IConfigurationDeploymentRollbackDispatcher,
        IConfigurationDeploymentObservationReader
    {
        private readonly object _gate = new();
        public List<RollbackConfigurationDeploymentCommand> Commands { get; } = [];
        public Dictionary<Guid, Guid> NewDeploymentIds { get; } = [];
        public Dictionary<Guid, ConfigurationDeploymentReadModel> Deployments { get; } = [];
        private readonly Dictionary<string, ConfigurationDeploymentRollbackResult> _resultsByIdempotencyKey = [];

        public Task<ApiResult<ConfigurationDeploymentRollbackResult>> HandleAsync(
            RollbackConfigurationDeploymentCommand command, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_resultsByIdempotencyKey.TryGetValue(command.IdempotencyKey, out var existing))
                    return Task.FromResult(ApiResult<ConfigurationDeploymentRollbackResult>.Success(existing));

                Commands.Add(command);
                var newDeploymentId = Guid.NewGuid();
                NewDeploymentIds[command.TargetDeploymentId] = newDeploymentId;
                Deployments[newDeploymentId] = new ConfigurationDeploymentReadModel
                {
                    Id = newDeploymentId,
                    Status = ConfigurationDeploymentReadStatus.Pending
                };
                var result = new ConfigurationDeploymentRollbackResult
                {
                    TargetDeploymentId = command.TargetDeploymentId,
                    NewDeploymentId = newDeploymentId,
                    KioskId = command.KioskId,
                    Status = "Pending"
                };
                _resultsByIdempotencyKey[command.IdempotencyKey] = result;
                return Task.FromResult(ApiResult<ConfigurationDeploymentRollbackResult>.Success(result));
            }
        }

        public Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
            Guid deploymentId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
                return Task.FromResult(Deployments.GetValueOrDefault(deploymentId));
        }

        public void FailAllPending(string failureCode, string failureReason)
        {
            lock (_gate)
                foreach (var deployment in Deployments.Values
                             .Where(item => item.Status == ConfigurationDeploymentReadStatus.Pending).ToArray())
                    Deployments[deployment.Id] = CopyWithStatus(
                        deployment, ConfigurationDeploymentReadStatus.Failed, failureCode, failureReason);
        }

        public void ActivateAllPending()
        {
            lock (_gate)
                foreach (var deployment in Deployments.Values
                             .Where(item => item.Status == ConfigurationDeploymentReadStatus.Pending).ToArray())
                    Deployments[deployment.Id] = CopyWithStatus(
                        deployment, ConfigurationDeploymentReadStatus.Active, null, null);
        }

        private static ConfigurationDeploymentReadModel CopyWithStatus(ConfigurationDeploymentReadModel source,
            ConfigurationDeploymentReadStatus status, string? failureCode, string? failureReason) => new()
        {
            Id = source.Id,
            Status = status,
            FailureCode = failureCode,
            FailureReason = failureReason
        };
    }

    private sealed class EmptyDeploymentObservationReader : IConfigurationDeploymentObservationReader
    {
        public Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
            Guid deploymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConfigurationDeploymentReadModel?>(null);
    }
}
