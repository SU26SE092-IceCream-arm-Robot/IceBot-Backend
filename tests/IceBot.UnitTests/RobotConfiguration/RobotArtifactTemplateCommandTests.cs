using Domain.RobotConfiguration.ArtifactTemplates;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Storage.Services;
using Domain.RobotConfiguration.Artifacts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using IceBot.UnitTests.TestSupport;
using Domain.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.ArtifactContracts;
using System.Security.Cryptography;
using System.Text;
using Application.Shared.Concurrency;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTemplateCommandTests
{
    [Fact]
    public async Task Publish_TransitionsDraftTemplateAndPersists()
    {
        var bytes = Encoding.UTF8.GetBytes("template");
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var contract = RobotArtifactTechnicalContract.CreateDraft("PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("PREPARE_EXECUTE", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        var template = RobotArtifactTemplate.CreateDraft(
            "PREPARE", "Prepare", "robot-artifact-templates/prepare.lua", "prepare.lua", checksum,
            "FAIRINO_LUA_V1", "FR5", bytes.Length, DateTimeOffset.UtcNow,
            technicalContractId: contract.Id, technicalContractChecksum: contract.ContractChecksum);
        var store = Substitute.For<IRobotArtifactTemplateStore>();
        store.GetByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);
        store.GetByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.ReadBytesAsync(template.StorageKey, template.ContentLengthBytes, Arg.Any<CancellationToken>())
            .Returns(bytes);
        var handler = new PublishRobotArtifactTemplateCommandHandler(
            store,
            new ArtifactPublicationValidator(contractStore, storage),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new PublishRobotArtifactTemplateCommand(template.Id)
        {
            UserContext = TestData.SystemAdmin()
        });

        Assert.True(result.Succeeded);
        Assert.Equal(RobotArtifactStatus.Published, template.Status);
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clone_CopiesPublishedTemplateIntoOrganizationDraft()
    {
        var organizationId = Guid.NewGuid();
        var contract = RobotArtifactTechnicalContract.CreateDraft("PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("PREPARE_EXECUTE", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        var template = TestData.DraftTemplate();
        template.AssignTechnicalContract(contract.Id, contract.ContractChecksum!);
        template.Publish();
        var templateStore = Substitute.For<IRobotArtifactTemplateStore>();
        templateStore.GetByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);
        var robotStore = Substitute.For<IRobotArtifactStore>();
        robotStore.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        robotStore.InsertArtifactOrGetExistingAsync(Arg.Any<RobotArtifact>(), Arg.Any<CancellationToken>())
            .Returns(call => new RobotArtifactInsertResult(true, call.Arg<RobotArtifact>()));
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.CopyImmutableAsync(
                template.StorageKey,
                Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ArtifactObjectWriteRequest>();
                return new ArtifactObjectWriteResult(request.StorageKey, request.Checksum, request.ContentLengthBytes);
            });
        var contentService = new ArtifactUploadContentService(
            storage,
            NullLogger<ArtifactUploadContentService>.Instance);
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var handler = new CloneRobotArtifactTemplateCommandHandler(
            robotStore,
            templateStore,
            storage,
            contentService,
            contractStore,
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new CloneRobotArtifactTemplateCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            TemplateId = template.Id,
            ArtifactCode = "ORG_PREPARE",
            ArtifactName = "Organization prepare"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(RobotArtifactStatus.Draft.ToString(), result.Data.Status);
        Assert.Equal(template.Id, result.Data.SourceRobotArtifactTemplateId);
        await storage.Received(1).CopyImmutableAsync(
            template.StorageKey,
            Arg.Any<ArtifactObjectWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clone_RejectsTemplateWhoseTechnicalContractWasRetired()
    {
        var organizationId = Guid.NewGuid();
        var contract = RobotArtifactTechnicalContract.CreateDraft("PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("PREPARE_EXECUTE", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        var template = TestData.DraftTemplate();
        template.AssignTechnicalContract(contract.Id, contract.ContractChecksum!);
        template.Publish();
        contract.Retire(DateTimeOffset.UtcNow, Guid.NewGuid());

        var templateStore = Substitute.For<IRobotArtifactTemplateStore>();
        templateStore.GetByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);
        var artifactStore = Substitute.For<IRobotArtifactStore>();
        artifactStore.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var handler = new CloneRobotArtifactTemplateCommandHandler(
            artifactStore,
            templateStore,
            storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            contractStore,
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new CloneRobotArtifactTemplateCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            TemplateId = template.Id,
            ArtifactCode = "ORG_PREPARE",
            ArtifactName = "Organization prepare"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await storage.DidNotReceive().CopyImmutableAsync(
            Arg.Any<string>(), Arg.Any<ArtifactObjectWriteRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clone_RejectsTemplateRetiredAfterInitialObservation()
    {
        var organizationId = Guid.NewGuid();
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("PREPARE_EXECUTE", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());

        var observedTemplate = TestData.DraftTemplate();
        observedTemplate.AssignTechnicalContract(contract.Id, contract.ContractChecksum!);
        observedTemplate.Publish();
        var lockedTemplate = TestData.DraftTemplate();
        lockedTemplate.Id = observedTemplate.Id;
        lockedTemplate.AssignTechnicalContract(contract.Id, contract.ContractChecksum!);
        lockedTemplate.Publish();
        lockedTemplate.Retire();

        var templateStore = Substitute.For<IRobotArtifactTemplateStore>();
        templateStore.GetByIdAsync(observedTemplate.Id, false, Arg.Any<CancellationToken>())
            .Returns(observedTemplate, lockedTemplate);
        var artifactStore = Substitute.For<IRobotArtifactStore>();
        artifactStore.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var handler = new CloneRobotArtifactTemplateCommandHandler(
            artifactStore,
            templateStore,
            storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            Substitute.For<IRobotArtifactTechnicalContractStore>(),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new CloneRobotArtifactTemplateCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            TemplateId = observedTemplate.Id,
            ArtifactCode = "ORG_PREPARE",
            ArtifactName = "Organization prepare"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await storage.DidNotReceive().CopyImmutableAsync(
            Arg.Any<string>(), Arg.Any<ArtifactObjectWriteRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_MapsOversizedObjectToIntegrityConflict()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft("PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("PREPARE_EXECUTE", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)],
            []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        var template = TestData.DraftTemplate();
        template.AssignTechnicalContract(contract.Id, contract.ContractChecksum!);
        var templateStore = Substitute.For<IRobotArtifactTemplateStore>();
        templateStore.GetByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);
        templateStore.GetByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        var contractStore = Substitute.For<IRobotArtifactTechnicalContractStore>();
        contractStore.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.ReadBytesAsync(template.StorageKey, template.ContentLengthBytes, Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => throw new ArtifactObjectSizeLimitExceededException(
                template.StorageKey, template.ContentLengthBytes));
        var handler = new PublishRobotArtifactTemplateCommandHandler(
            templateStore,
            new ArtifactPublicationValidator(contractStore, storage),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new PublishRobotArtifactTemplateCommand(template.Id)
        {
            UserContext = TestData.SystemAdmin()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(RobotArtifactStatus.Draft, template.Status);
    }

    [Fact]
    public async Task Discard_DeletesDraftMetadataAndObject()
    {
        var template = TestData.DraftTemplate();
        var store = Substitute.For<IRobotArtifactTemplateStore>();
        store.GetByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        store.DiscardDraftAsync(template, Arg.Any<CancellationToken>())
            .Returns(RobotArtifactTemplateDiscardOutcome.Deleted);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var contentService = new ArtifactUploadContentService(
            storage,
            NullLogger<ArtifactUploadContentService>.Instance);
        var handler = new DiscardDraftRobotArtifactTemplateCommandHandler(
            store, contentService, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new DiscardDraftRobotArtifactTemplateCommand(template.Id)
        {
            UserContext = TestData.SystemAdmin()
        });

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.ObjectDeleted);
        await storage.Received(1).DeleteIfExistsAsync(template.StorageKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkUpload_MapsCompleteStorageOutageToServiceUnavailable()
    {
        var bytes = Encoding.UTF8.GetBytes("print('template')");
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.WriteImmutableAsync(
                Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<ArtifactObjectWriteResult>>(_ =>
                throw new ArtifactObjectStorageUnavailableException(
                    "storage unavailable", new IOException("test outage")));
        var itemHandler = new UploadRobotArtifactTemplateCommandHandler(
            Substitute.For<IRobotArtifactTemplateStore>(),
            new ArtifactUploadContentService(
                storage,
                NullLogger<ArtifactUploadContentService>.Instance));
        var handler = new BulkUploadRobotArtifactTemplatesCommandHandler(itemHandler);

        var result = await handler.HandleAsync(new BulkUploadRobotArtifactTemplatesCommand
        {
            UserContext = TestData.SystemAdmin(),
            Items =
            [
                new UploadRobotArtifactTemplateCommand
                {
                    UserContext = TestData.SystemAdmin(),
                    FileName = "offline.lua",
                    ContentType = "text/plain",
                    ContentLengthBytes = bytes.Length,
                    Content = new MemoryStream(bytes),
                    TemplateCode = "OFFLINE",
                    TemplateName = "Offline",
                    RuntimeTargetCode = "FAIRINO_LUA_V1",
                    MachineModelCode = "FR5"
                }
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(503, Assert.Single(result.Data!.Items).StatusCode);
    }
}
