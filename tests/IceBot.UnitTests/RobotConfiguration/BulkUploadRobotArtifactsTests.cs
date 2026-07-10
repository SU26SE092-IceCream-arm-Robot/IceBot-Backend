using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using System.Text;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Storage.Services;
using Domain.RobotConfiguration.Artifacts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using IceBot.UnitTests.TestSupport;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class BulkUploadRobotArtifactsTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMultiStatusAndKeepsSuccessfulItems()
    {
        var organizationId = Guid.NewGuid();
        var store = CreateStore(organizationId);
        var storage = CreateStorage();
        store.InsertArtifactOrGetExistingAsync(Arg.Any<RobotArtifact>(), Arg.Any<CancellationToken>())
            .Returns(call => new RobotArtifactInsertResult(true, call.Arg<RobotArtifact>()));
        var handler = CreateHandler(store, storage);

        var result = await handler.HandleAsync(new BulkUploadRobotArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            Items =
            [
                Item("valid.lua", "VALID"),
                Item("invalid.txt", "INVALID")
            ]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(207, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.SucceededCount);
        Assert.Equal(1, result.Data.FailedCount);
        Assert.Equal(1, result.Data.UploadedCount);
        await store.Received(1).InsertArtifactOrGetExistingAsync(
            Arg.Any<RobotArtifact>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsExistingArtifactWithoutWritingObject()
    {
        var organizationId = Guid.NewGuid();
        var store = CreateStore(organizationId);
        var storage = CreateStorage();
        var existing = TestData.PublishedArtifact(organizationId, "EXISTING", "existing.lua");
        store.GetArtifactByCodeAndChecksumAsync(
                organizationId,
                "EXISTING",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = CreateHandler(store, storage);

        var result = await handler.HandleAsync(new BulkUploadRobotArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            Items = [Item("existing.lua", "EXISTING")]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(1, result.Data!.ExistingCount);
        Assert.True(result.Data.Items.Single().WasExisting);
        await storage.DidNotReceive().WriteImmutableAsync(
            Arg.Any<ArtifactObjectWriteRequest>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    private static BulkUploadRobotArtifactsCommandHandler CreateHandler(
        IRobotArtifactStore store,
        IArtifactObjectStorage storage)
    {
        var contentService = new ArtifactUploadContentService(
            storage,
            NullLogger<ArtifactUploadContentService>.Instance);
        return new BulkUploadRobotArtifactsCommandHandler(
            new UploadRobotArtifactCommandHandler(store, contentService));
    }

    private static IRobotArtifactStore CreateStore(Guid organizationId)
    {
        var store = Substitute.For<IRobotArtifactStore>();
        store.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        return store;
    }

    private static IArtifactObjectStorage CreateStorage()
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.WriteImmutableAsync(
                Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ArtifactObjectWriteRequest>();
                return new ArtifactObjectWriteResult(
                    request.StorageKey,
                    request.Checksum,
                    request.ContentLengthBytes);
            });
        return storage;
    }

    private static BulkUploadRobotArtifactItem Item(string fileName, string code)
    {
        var bytes = Encoding.UTF8.GetBytes("print('icebot')");
        return new BulkUploadRobotArtifactItem
        {
            FileName = fileName,
            ContentType = "text/plain",
            ContentLengthBytes = bytes.Length,
            Content = new MemoryStream(bytes),
            ArtifactCode = code,
            ArtifactName = code,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5"
        };
    }
}
