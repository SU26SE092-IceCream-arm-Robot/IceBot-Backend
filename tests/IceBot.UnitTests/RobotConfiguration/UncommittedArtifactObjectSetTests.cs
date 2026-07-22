using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class UncommittedArtifactObjectSetTests
{
    [Fact]
    public async Task DisposeWithoutCommit_CompensatesEveryTrackedObjectOnce()
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        var content = new ArtifactUploadContentService(
            storage, NullLogger<ArtifactUploadContentService>.Instance);

        await using (var objects = new UncommittedArtifactObjectSet(content))
        {
            objects.Track("staging/a.lua");
            objects.Track("staging/a.lua");
            objects.Track("staging/b.lua");
        }

        await storage.Received(1).DeleteIfExistsAsync("staging/a.lua", CancellationToken.None);
        await storage.Received(1).DeleteIfExistsAsync("staging/b.lua", CancellationToken.None);
    }

    [Fact]
    public async Task Commit_PreventsCompensationAndFurtherTracking()
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        var content = new ArtifactUploadContentService(
            storage, NullLogger<ArtifactUploadContentService>.Instance);
        var objects = new UncommittedArtifactObjectSet(content);
        objects.Track("committed/a.lua");

        objects.Commit();
        await objects.DisposeAsync();

        await storage.DidNotReceive().DeleteIfExistsAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Throws<InvalidOperationException>(() => objects.Track("late.lua"));
    }
}
