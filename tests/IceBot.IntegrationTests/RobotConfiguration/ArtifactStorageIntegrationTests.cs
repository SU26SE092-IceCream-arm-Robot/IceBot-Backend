using System.Net;
using System.Text;
using Application.RobotConfiguration.Abstractions;
using IceBot.IntegrationTests.Infrastructure;

namespace IceBot.IntegrationTests.RobotConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ArtifactStorageIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public ArtifactStorageIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task PresignedReadUrl_ReturnsUploadedLuaBytes()
    {
        var storage = _fixture.CreateObjectStorage(downloadUrlExpirySeconds: 60);
        var bytes = Encoding.UTF8.GetBytes("print('presigned')");
        var checksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        var key = $"robot-artifacts/integration/{Guid.NewGuid():N}/{checksum}.lua";
        await using var content = new MemoryStream(bytes);
        await storage.WriteImmutableAsync(
            new ArtifactObjectWriteRequest(key, "text/plain", bytes.Length, checksum),
            content);

        var readUrl = await storage.CreateReadUrlAsync(key);
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(readUrl.Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        Assert.True(readUrl.ExpiresAt > DateTimeOffset.UtcNow);
    }
}
