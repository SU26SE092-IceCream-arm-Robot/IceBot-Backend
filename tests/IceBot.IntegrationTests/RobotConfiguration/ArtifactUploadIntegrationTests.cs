using System.Text;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Services;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.RobotConfiguration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IceBot.IntegrationTests.RobotConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ArtifactUploadIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public ArtifactUploadIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task DatabaseFailure_AfterObjectWrite_LeavesObjectForOrphanCleanup()
    {
        var organizationId = await SeedOrganizationAsync();
        var storage = _fixture.CreateObjectStorage();
        await EnsureBucketAsync(storage);
        await using var dbContext = _fixture.CreateDbContext();
        var handler = CreateHandler(dbContext, storage);
        var command = UploadCommand(organizationId, new string('X', 501), "db-failure.lua");

        await Assert.ThrowsAsync<DbUpdateException>(() => handler.HandleAsync(command));

        var objects = await ListObjectsAsync(storage, $"robot-artifacts/{organizationId:D}/");
        Assert.Single(objects);
        Assert.False(await dbContext.RobotArtifacts.AnyAsync(artifact => artifact.OrganizationId == organizationId));
    }

    [IntegrationFact]
    public async Task ConcurrentSameIdentityUpload_ReturnsOneCommittedArtifactAndOneObject()
    {
        var organizationId = await SeedOrganizationAsync();
        var storage = _fixture.CreateObjectStorage();
        await EnsureBucketAsync(storage);
        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var firstHandler = CreateHandler(firstContext, storage);
        var secondHandler = CreateHandler(secondContext, storage);

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(UploadCommand(organizationId, "CONCURRENT", "concurrent.lua")),
            secondHandler.HandleAsync(UploadCommand(organizationId, "CONCURRENT", "concurrent.lua")));

        Assert.All(results, result => Assert.True(result.Succeeded));
        Assert.Single(results.Select(result => result.Data!.Id).Distinct());
        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.RobotArtifacts.CountAsync(
            artifact => artifact.OrganizationId == organizationId && artifact.ArtifactCode == "CONCURRENT"));
        var objects = await ListObjectsAsync(storage, $"robot-artifacts/{organizationId:D}/");
        Assert.Single(objects);
    }

    private UploadRobotArtifactCommandHandler CreateHandler(
        global::Infrastructure.Data.IceBotDbContext dbContext,
        Application.RobotConfiguration.Abstractions.IArtifactObjectStorage storage) =>
        new(
            new RobotConfigurationStore(dbContext),
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance));

    private static UploadRobotArtifactCommand UploadCommand(Guid organizationId, string code, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes("print('same-content')");
        return new UploadRobotArtifactCommand
        {
            UserContext = new CurrentUserContext { AccountId = Guid.NewGuid(), IsSystemAdmin = true },
            OrganizationId = organizationId,
            ArtifactCode = code,
            ArtifactName = code,
            FileName = fileName,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5",
            ContentType = "text/plain",
            ContentLengthBytes = bytes.Length,
            Content = new MemoryStream(bytes)
        };
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Integration organization",
            Status = EntityStatus.Active
        };
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private static async Task EnsureBucketAsync(
        Application.RobotConfiguration.Abstractions.IArtifactObjectStorage storage)
    {
        var bytes = Encoding.UTF8.GetBytes("bucket-ready");
        var key = $"integration-bootstrap/{Guid.NewGuid():N}";
        await using var content = new MemoryStream(bytes);
        await storage.WriteImmutableAsync(
            new Application.RobotConfiguration.Abstractions.ArtifactObjectWriteRequest(
                key,
                "application/octet-stream",
                bytes.Length,
                new string('0', 64)),
            content);
        await storage.DeleteIfExistsAsync(key);
    }

    private static async Task<IReadOnlyList<Application.RobotConfiguration.Abstractions.ArtifactObjectInfo>> ListObjectsAsync(
        Application.RobotConfiguration.Abstractions.IArtifactObjectStorage storage,
        string prefix)
    {
        var results = new List<Application.RobotConfiguration.Abstractions.ArtifactObjectInfo>();
        await foreach (var item in storage.ListAsync(prefix))
        {
            results.Add(item);
        }

        return results;
    }
}
