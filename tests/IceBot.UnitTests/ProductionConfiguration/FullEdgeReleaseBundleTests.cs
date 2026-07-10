using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class FullEdgeReleaseBundleTests
{
    [Fact]
    public async Task BuildsImmutableZipWithManifestAndVerifiedLuaArtifact()
    {
        var artifactId = Guid.NewGuid();
        var lua = Encoding.UTF8.GetBytes("print('bundle')");
        var checksum = Convert.ToHexString(SHA256.HashData(lua)).ToLowerInvariant();
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.ReadBytesAsync("robot-artifacts/source.lua", lua.Length, Arg.Any<CancellationToken>()).Returns(lua);
        storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        byte[]? writtenBytes = null;
        ArtifactObjectWriteRequest? writeRequest = null;
        storage.WriteImmutableAsync(
                Arg.Any<ArtifactObjectWriteRequest>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                writeRequest = call.ArgAt<ArtifactObjectWriteRequest>(0);
                var stream = call.ArgAt<Stream>(1);
                await using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                writtenBytes = copy.ToArray();
                return new ArtifactObjectWriteResult(writeRequest.StorageKey, writeRequest.Checksum, writeRequest.ContentLengthBytes);
            });
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 3);
        var program = new PublishedRobotProgramSnapshot(
            Guid.NewGuid(),
            "MAKE",
            release.OrganizationId,
            1,
            new string('b', 64),
            [new PublishedRobotArtifactSnapshot(
                Guid.NewGuid(), artifactId, 1, 1, null, checksum, "robot-artifacts/source.lua",
                "FAIRINO_LUA_V1", "FR5", lua.Length)]);

        var descriptor = await new FullEdgeReleaseBundleService(storage).BuildAndStoreAsync(
            release,
            new Dictionary<Guid, PublishedRobotProgramSnapshot> { [program.Id] = program },
            "{\"ExecutionRoutes\":[]}");

        Assert.NotNull(writtenBytes);
        Assert.Equal("application/zip", writeRequest!.ContentType);
        Assert.Equal(descriptor.Checksum, Convert.ToHexString(SHA256.HashData(writtenBytes!)).ToLowerInvariant());
        using var archive = new ZipArchive(new MemoryStream(writtenBytes!), ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry($"artifacts/{artifactId:D}.lua"));
        var manifestEntry = Assert.Single(archive.Entries, entry => entry.FullName == "release-content-manifest.json");
        using var manifest = JsonDocument.Parse(manifestEntry.Open());
        Assert.Equal(0, manifest.RootElement.GetProperty("ExecutionRoutes").GetArrayLength());
    }
}
