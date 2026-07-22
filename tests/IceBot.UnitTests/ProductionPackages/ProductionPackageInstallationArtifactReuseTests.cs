using Application.Identity.Tokens.Claims;
using Application.ProductionPackages;
using Application.ProductionPackages.Installation;
using Application.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Concurrency;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Cryptography;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageInstallationArtifactReuseTests
{
    [Fact]
    public async Task Install_ReusesCompatibleOrganizationArtifactAcrossKioskScopes()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var contract = PublishedContract();
        var template = PublishedTemplate(contract);
        var version = PublishedVersion(template, contract);
        var existingArtifact = RobotArtifact.CreateDraft(
            organizationId, "BASE", "Base", "robot-artifacts/existing.lua", template.FileName,
            template.Checksum, template.RuntimeTargetCode, template.MachineModelCode,
            template.ContentLengthBytes, template.ExportedAt,
            sourceRobotArtifactTemplateId: template.Id,
            technicalContractId: contract.Id,
            technicalContractChecksum: contract.ContractChecksum);

        var packages = Substitute.For<IProductionPackageStore>();
        packages.GetVersionAsync(version.ProductionPackageId, version.Id, false, Arg.Any<CancellationToken>())
            .Returns(version);
        packages.LoadTechnicalContractsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contract]);
        packages.LoadArtifactTemplatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([template]);

        var persistedArtifacts = Array.Empty<RobotArtifact>();
        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.ScopeExistsAsync(organizationId, storeId, kioskId, Arg.Any<CancellationToken>())
            .Returns(true);
        installations.InsertOrGetAsync(Arg.Any<ProductionPackageInstallation>(), Arg.Any<CancellationToken>())
            .Returns(call => new ProductionPackageInstallationInsertResult(
                true, call.Arg<ProductionPackageInstallation>()));
        installations.ListArtifactsByCodesAsync(
                organizationId, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([existingArtifact]);
        installations.ListPackageManagedArtifactIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { existingArtifact.Id });
        installations.PersistMaterializedGraphAsync(
                Arg.Any<ProductionPackageInstallation>(),
                Arg.Any<IReadOnlyCollection<Domain.Catalog.Entities.Product>>(),
                Arg.Any<IReadOnlyCollection<RobotArtifact>>(),
                Arg.Any<IReadOnlyCollection<Domain.RobotConfiguration.Programs.RobotProgram>>(),
                Arg.Any<IReadOnlyCollection<ProductionComposition>>(),
                Arg.Any<Func<long, ConfigurationRelease>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                persistedArtifacts = call.ArgAt<IReadOnlyCollection<RobotArtifact>>(2).ToArray();
                var installation = call.ArgAt<ProductionPackageInstallation>(0);
                var release = call.ArgAt<Func<long, ConfigurationRelease>>(5)(1);
                installation.Complete(release.Id, DateTimeOffset.UtcNow);
                return release;
            });

        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.ReadBytesAsync(existingArtifact.StorageKey, existingArtifact.ContentLengthBytes,
                Arg.Any<CancellationToken>())
            .Returns(ArtifactBytes);
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var coordinator = new RecordingMutationCoordinator();
        var service = new ProductionPackageInstallationService(
            packages,
            installations,
            storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(contractStore, storage),
            coordinator);

        var result = await service.InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PackageId = version.ProductionPackageId,
            PackageVersionId = version.Id,
            IdempotencyKey = "install-kiosk-2"
        }, CancellationToken.None);

        Assert.True(result.Succeeded, result.Message);
        Assert.Empty(persistedArtifacts);
        Assert.Contains(result.Data!.Materializations,
            item => item.ResourceKind == ProductionPackageResourceKind.RobotArtifact.ToString() &&
                item.TargetKey == existingArtifact.Id.ToString("D"));
        await storage.DidNotReceive().CopyImmutableAsync(
            Arg.Any<string>(), Arg.Any<ArtifactObjectWriteRequest>(), Arg.Any<CancellationToken>());
        Assert.Contains(coordinator.Resources.SelectMany(resources => resources),
            identity => identity == TechnicalResourceMutationIdentity.ArtifactDefinition(organizationId, "BASE"));
        Assert.Contains(coordinator.Resources.SelectMany(resources => resources),
            identity => identity == TechnicalResourceMutationIdentity.Artifact(existingArtifact.Id));
        Assert.Contains(coordinator.Resources.SelectMany(resources => resources),
            identity => identity == TechnicalResourceMutationIdentity.Template(template.Id));
        Assert.Contains(coordinator.Resources.SelectMany(resources => resources),
            identity => identity == TechnicalResourceMutationIdentity.Contract(contract.Id));
    }

    [Fact]
    public async Task Install_RejectsCompatibleOrganizationAuthoredArtifactInsteadOfTakingOwnership()
    {
        var organizationId = Guid.NewGuid();
        var contract = PublishedContract();
        var template = PublishedTemplate(contract);
        var version = PublishedVersion(template, contract);
        var existingArtifact = RobotArtifact.CreateDraft(
            organizationId, "BASE", "Base", "robot-artifacts/manual.lua", template.FileName,
            template.Checksum, template.RuntimeTargetCode, template.MachineModelCode,
            template.ContentLengthBytes, template.ExportedAt,
            sourceRobotArtifactTemplateId: template.Id,
            technicalContractId: contract.Id,
            technicalContractChecksum: contract.ContractChecksum);
        var packages = Substitute.For<IProductionPackageStore>();
        packages.GetVersionAsync(version.ProductionPackageId, version.Id, false, Arg.Any<CancellationToken>())
            .Returns(version);
        packages.LoadTechnicalContractsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contract]);
        packages.LoadArtifactTemplatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([template]);
        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.ScopeExistsAsync(organizationId, null, null, Arg.Any<CancellationToken>()).Returns(true);
        installations.InsertOrGetAsync(Arg.Any<ProductionPackageInstallation>(), Arg.Any<CancellationToken>())
            .Returns(call => new ProductionPackageInstallationInsertResult(true,
                call.Arg<ProductionPackageInstallation>()));
        installations.ListArtifactsByCodesAsync(
                organizationId, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([existingArtifact]);
        installations.ListPackageManagedArtifactIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
        var storage = Substitute.For<IArtifactObjectStorage>();
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        var service = new ProductionPackageInstallationService(
            packages, installations, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(contractStore, storage),
            new RecordingMutationCoordinator());

        var result = await service.InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            OrganizationId = organizationId,
            PackageId = version.ProductionPackageId,
            PackageVersionId = version.Id,
            IdempotencyKey = "manual-conflict"
        }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("organization-authored artifact", result.Message);
        await installations.DidNotReceive().PersistMaterializedGraphAsync(
            Arg.Any<ProductionPackageInstallation>(), Arg.Any<IReadOnlyCollection<Domain.Catalog.Entities.Product>>(),
            Arg.Any<IReadOnlyCollection<RobotArtifact>>(), Arg.Any<IReadOnlyCollection<RobotProgram>>(),
            Arg.Any<IReadOnlyCollection<ProductionComposition>>(),
            Arg.Any<Func<long, ConfigurationRelease>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_LosingConcurrentRetryCleansPreparedObjectAndReturnsTerminalInstallation()
    {
        var organizationId = Guid.NewGuid();
        var contract = PublishedContract();
        var template = PublishedTemplate(contract);
        var version = PublishedVersion(template, contract);
        var packages = Substitute.For<IProductionPackageStore>();
        packages.GetVersionAsync(version.ProductionPackageId, version.Id, false, Arg.Any<CancellationToken>())
            .Returns(version);
        packages.LoadTechnicalContractsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contract]);
        packages.LoadArtifactTemplatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([template]);

        ProductionPackageInstallation? installation = null;
        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.ScopeExistsAsync(organizationId, null, null, Arg.Any<CancellationToken>()).Returns(true);
        installations.InsertOrGetAsync(Arg.Any<ProductionPackageInstallation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                installation = call.Arg<ProductionPackageInstallation>();
                return new ProductionPackageInstallationInsertResult(true, installation);
            });
        installations.ListArtifactsByCodesAsync(
                organizationId, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        installations.ListPackageManagedArtifactIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
        installations.GetCurrentStatusAsync(organizationId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                installation!.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
                return ProductionPackageInstallationStatus.Installed;
            });
        installations.GetAsync(organizationId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => installation);

        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.CopyImmutableAsync(template.StorageKey, Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.ArgAt<ArtifactObjectWriteRequest>(1);
                return new ArtifactObjectWriteResult(
                    request.StorageKey, request.Checksum, request.ContentLengthBytes);
            });
        storage.DeleteIfExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var service = new ProductionPackageInstallationService(
            packages, installations, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(Substitute.For<IRobotArtifactTechnicalContractStore>(), storage),
            new RecordingMutationCoordinator());

        var result = await service.InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            OrganizationId = organizationId,
            PackageId = version.ProductionPackageId,
            PackageVersionId = version.Id,
            IdempotencyKey = "concurrent-terminal-winner"
        }, CancellationToken.None);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(nameof(ProductionPackageInstallationStatus.Installed), result.Data!.Status);
        await storage.Received(1).CopyImmutableAsync(
            template.StorageKey, Arg.Any<ArtifactObjectWriteRequest>(), Arg.Any<CancellationToken>());
        await storage.Received(1).DeleteIfExistsAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await installations.DidNotReceive().PersistMaterializedGraphAsync(
            Arg.Any<ProductionPackageInstallation>(), Arg.Any<IReadOnlyCollection<Domain.Catalog.Entities.Product>>(),
            Arg.Any<IReadOnlyCollection<RobotArtifact>>(), Arg.Any<IReadOnlyCollection<RobotProgram>>(),
            Arg.Any<IReadOnlyCollection<ProductionComposition>>(),
            Arg.Any<Func<long, ConfigurationRelease>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_DatabaseFailureAfterObjectCopyCompensatesAndMarksInstallationFailed()
    {
        var organizationId = Guid.NewGuid();
        var contract = PublishedContract();
        var template = PublishedTemplate(contract);
        var version = PublishedVersion(template, contract);
        var packages = Substitute.For<IProductionPackageStore>();
        packages.GetVersionAsync(version.ProductionPackageId, version.Id, false, Arg.Any<CancellationToken>())
            .Returns(version);
        packages.LoadTechnicalContractsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contract]);
        packages.LoadArtifactTemplatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([template]);

        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.ScopeExistsAsync(organizationId, null, null, Arg.Any<CancellationToken>()).Returns(true);
        installations.InsertOrGetAsync(Arg.Any<ProductionPackageInstallation>(), Arg.Any<CancellationToken>())
            .Returns(call => new ProductionPackageInstallationInsertResult(
                true, call.Arg<ProductionPackageInstallation>()));
        installations.ListArtifactsByCodesAsync(
                organizationId, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        installations.ListPackageManagedArtifactIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
        installations.PersistMaterializedGraphAsync(
                Arg.Any<ProductionPackageInstallation>(),
                Arg.Any<IReadOnlyCollection<Domain.Catalog.Entities.Product>>(),
                Arg.Any<IReadOnlyCollection<RobotArtifact>>(),
                Arg.Any<IReadOnlyCollection<RobotProgram>>(),
                Arg.Any<IReadOnlyCollection<ProductionComposition>>(),
                Arg.Any<Func<long, ConfigurationRelease>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ConfigurationRelease>(
                new DbUpdateException("Simulated transaction failure.")));

        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.CopyImmutableAsync(template.StorageKey, Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.ArgAt<ArtifactObjectWriteRequest>(1);
                return new ArtifactObjectWriteResult(
                    request.StorageKey, request.Checksum, request.ContentLengthBytes);
            });
        storage.DeleteIfExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var service = new ProductionPackageInstallationService(
            packages, installations, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(Substitute.For<IRobotArtifactTechnicalContractStore>(), storage),
            new RecordingMutationCoordinator());

        var result = await service.InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            OrganizationId = organizationId,
            PackageId = version.ProductionPackageId,
            PackageVersionId = version.Id,
            IdempotencyKey = "db-failure-after-copy"
        }, CancellationToken.None);

        Assert.False(result.Succeeded);
        await storage.Received(1).DeleteIfExistsAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await installations.Received(1).MarkFailedAsync(
            organizationId, Arg.Any<Guid>(), "PackageMaterializationFailed",
            Arg.Is<string>(message => message.Contains("Simulated transaction failure")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fork_CopiesSharedArtifactAndRetargetsInstallationProgram()
    {
        var organizationId = Guid.NewGuid();
        var contract = PublishedContract();
        var template = PublishedTemplate(contract);
        var source = RobotArtifact.CreateDraft(
            organizationId, "BASE", "Base", "robot-artifacts/shared.lua", template.FileName,
            template.Checksum, template.RuntimeTargetCode, template.MachineModelCode,
            template.ContentLengthBytes, template.ExportedAt,
            sourceRobotArtifactTemplateId: template.Id,
            technicalContractId: contract.Id,
            technicalContractChecksum: contract.ContractChecksum);
        var program = RobotProgram.CreateDraft("PKG_1_STANDARD", "Standard", TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(source.Id, 1);
        var installation = ProductionPackageInstallation.Start(
            organizationId, null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            "fork-install", ["ICE_CREAM"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.AddMaterialization(ProductionPackageResourceKind.RobotArtifact, "BASE",
            source.Id.ToString("D"), source.Checksum);
        installation.AddMaterialization(ProductionPackageResourceKind.RobotProgram, "STANDARD",
            program.Id.ToString("D"));
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var graph = new ProductionPackageForkGraph(
            installation, [source], [program], new HashSet<Guid> { source.Id });
        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.GetForkGraphAsync(organizationId, installation.Id, false, Arg.Any<CancellationToken>())
            .Returns(graph);
        installations.GetForkGraphAsync(organizationId, installation.Id, true, Arg.Any<CancellationToken>())
            .Returns(graph);
        IReadOnlyCollection<RobotArtifact> persisted = [];
        installations.PersistForkAsync(installation, Arg.Any<IReadOnlyCollection<RobotArtifact>>(),
                Arg.Any<IReadOnlyCollection<RobotProgramArtifact>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                persisted = call.ArgAt<IReadOnlyCollection<RobotArtifact>>(1);
                return Task.CompletedTask;
            });
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.ReadBytesAsync(source.StorageKey, source.ContentLengthBytes, Arg.Any<CancellationToken>())
            .Returns(ArtifactBytes);
        storage.CopyImmutableAsync(source.StorageKey, Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.ArgAt<ArtifactObjectWriteRequest>(1);
                return new ArtifactObjectWriteResult(request.StorageKey, request.Checksum,
                    request.ContentLengthBytes);
            });
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var service = new ProductionPackageInstallationService(
            Substitute.For<IProductionPackageStore>(), installations, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(contractStore, storage),
            new RecordingMutationCoordinator());

        var result = await service.ForkAsync(
            new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            organizationId, installation.Id, CancellationToken.None);

        Assert.True(result.Succeeded, result.Message);
        var clone = Assert.Single(persisted);
        Assert.NotEqual(source.Id, clone.Id);
        Assert.Equal(clone.Id, Assert.Single(program.RobotProgramArtifacts).RobotArtifactId);
        Assert.Contains(installation.Materializations,
            item => item.ResourceKind == ProductionPackageResourceKind.RobotArtifact &&
                item.TargetKey == clone.Id.ToString("D"));
        Assert.Equal(ProductionPackageOwnershipMode.OrganizationFork, installation.OwnershipMode);
    }

    [Fact]
    public async Task Fork_IsRejected_WhenInstallationParticipatesInActiveUpgrade()
    {
        var organizationId = Guid.NewGuid();
        var installation = ProductionPackageInstallation.Start(
            organizationId, null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            "active-upgrade", ["ICE_CREAM"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var installations = Substitute.For<IProductionPackageInstallationStore>();
        installations.GetForkGraphAsync(organizationId, installation.Id, false, Arg.Any<CancellationToken>())
            .Returns(new ProductionPackageForkGraph(installation, [], [], new HashSet<Guid>()));
        installations.HasActiveUpgradeAsync(organizationId, installation.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var service = new ProductionPackageInstallationService(
            Substitute.For<IProductionPackageStore>(), installations, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(Substitute.For<IRobotArtifactTechnicalContractStore>(), storage),
            new RecordingMutationCoordinator());

        var result = await service.ForkAsync(
            new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            organizationId, installation.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("active upgrade", result.Message, StringComparison.OrdinalIgnoreCase);
        await installations.DidNotReceive().PersistForkAsync(
            Arg.Any<ProductionPackageInstallation>(),
            Arg.Any<IReadOnlyCollection<RobotArtifact>>(),
            Arg.Any<IReadOnlyCollection<RobotProgramArtifact>>(),
            Arg.Any<CancellationToken>());
    }

    private static RobotArtifactTechnicalContract PublishedContract()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "BASE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("MAKE_BASE", RobotArtifactEffectKind.Motion, null, null,
                RobotArtifactQuantityMode.None, null, null, "ROBOT_ARM")],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid(), parameterizedRuntimeSupported: false);
        return contract;
    }

    private static RobotArtifactTemplate PublishedTemplate(RobotArtifactTechnicalContract contract)
    {
        var template = RobotArtifactTemplate.CreateDraft(
            "BASE", "Base", "robot-artifact-templates/base.lua", "base.lua", ArtifactChecksum,
            "FAIRINO_LUA_V1", "FR5", ArtifactBytes.LongLength, DateTimeOffset.UtcNow,
            technicalContractId: contract.Id,
            technicalContractChecksum: contract.ContractChecksum);
        template.Publish();
        return template;
    }

    private static readonly byte[] ArtifactBytes = "return true"u8.ToArray();
    private static string ArtifactChecksum =>
        Convert.ToHexString(SHA256.HashData(ArtifactBytes)).ToLowerInvariant();

    private static ProductionPackageVersion PublishedVersion(
        RobotArtifactTemplate template,
        RobotArtifactTechnicalContract contract)
    {
        var packageId = Guid.NewGuid();
        var product = ProductionPackageProductDefinition.Create("ICE_CREAM", Guid.NewGuid(), $$"""
            {
              "SchemaVersion": 2,
              "Product": {
                "Id": "{{Guid.NewGuid()}}", "Code": "ICE_CREAM", "Name": "Ice cream",
                "Currency": "VND",
                "Variants": [{
                  "Id": "{{Guid.NewGuid()}}", "Code": "STANDARD", "Name": "Standard",
                  "FulfillmentType": 1,
                  "Recipes": [{ "Id": "{{Guid.NewGuid()}}", "Code": "DEFAULT", "Name": "Default",
                    "Version": 1, "IsDefault": true, "YieldQuantity": 1, "Unit": "serving", "Items": [] }]
                }],
                "OptionGroups": []
              }
            }
            """);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "BASE", template.Id, template.Checksum, contract.Id, contract.ContractChecksum!);
        var program = ProductionPackageProgramBlueprint.Create(
            "STANDARD", "FAIRINO_LUA_V1", "FR5",
            [("BASE", "BASE", "MAKE_BASE", "BASE", true, false, 1)]);
        var route = ProductionPackageRouteBlueprint.Create(
            "STANDARD", "ICE_CREAM", "STANDARD", "DEFAULT", [], "STANDARD",
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
        var version = ProductionPackageVersion.CreateDraft(packageId, 1);
        version.ReplaceDefinition([product], [artifact], [program], [route]);
        version.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        return version;
    }

    private sealed class RecordingMutationCoordinator : ITechnicalResourceMutationCoordinator
    {
        public List<IReadOnlyCollection<TechnicalResourceMutationIdentity>> Resources { get; } = [];

        public Task<T> ExecuteAsync<T>(
            IReadOnlyCollection<TechnicalResourceMutationIdentity> resources,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            Resources.Add(resources);
            return action(cancellationToken);
        }
    }
}
