using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Services;
using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using IceBot.UnitTests.TestSupport;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTemplateCommandTests
{
    [Fact]
    public async Task Publish_TransitionsDraftTemplateAndPersists()
    {
        var template = TestData.DraftTemplate();
        var store = Substitute.For<IRobotArtifactTemplateStore>();
        store.GetByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        var handler = new PublishRobotArtifactTemplateCommandHandler(store);

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
        var template = TestData.DraftTemplate();
        template.Publish();
        var templateStore = Substitute.For<IRobotArtifactTemplateStore>();
        templateStore.GetByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);
        var robotStore = Substitute.For<IRobotConfigurationStore>();
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
        var handler = new CloneRobotArtifactTemplateCommandHandler(
            robotStore,
            templateStore,
            storage,
            contentService);

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
        var handler = new DiscardDraftRobotArtifactTemplateCommandHandler(store, contentService);

        var result = await handler.HandleAsync(new DiscardDraftRobotArtifactTemplateCommand(template.Id)
        {
            UserContext = TestData.SystemAdmin()
        });

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.ObjectDeleted);
        await storage.Received(1).DeleteIfExistsAsync(template.StorageKey, Arg.Any<CancellationToken>());
    }
}
