using System.Net;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;

namespace IceBot.IntegrationTests.RobotConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class RobotAuthoringImportApiIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task OrganizationInboxIsPagedSafeAndRejectsForeignOrganization()
    {
        var actorId = Guid.NewGuid();
        var organization = Organization("IMPORT-API");
        var foreignOrganization = Organization("IMPORT-FOREIGN");
        await using (var seed = fixture.CreateDbContext())
        {
            seed.Add(new Account
            {
                Id = actorId,
                UserName = $"import-api-{Guid.NewGuid():N}",
                Email = $"import-api-{Guid.NewGuid():N}@example.test",
                Status = AccountStatus.Active
            });
            seed.AddRange(organization, foreignOrganization);
            seed.RobotAuthoringImports.AddRange(
                Import(organization.Id, actorId, "MAKE_COFFEE"),
                Import(foreignOrganization.Id, actorId, "MAKE_TEA"));
            await seed.SaveChangesAsync();
        }

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            fixture.CreateObjectStorage(autoCreateBucket: true),
            actorId,
            "OrgAdmin",
            [$"OrgAdmin|{organization.Id:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            $"/api/v1/management/organizations/{organization.Id:D}/robot-authoring-imports?status=Uploaded&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var data = document.RootElement.GetProperty("data");
        var item = Assert.Single(data.EnumerateArray());
        Assert.Equal("MAKE_COFFEE", item.GetProperty("proposedProgramCode").GetString());
        Assert.False(item.TryGetProperty("importChecksum", out _));
        Assert.False(item.TryGetProperty("items", out _));
        Assert.Equal(1, document.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());

        using var forbidden = await client.GetAsync(
            $"/api/v1/management/organizations/{foreignOrganization.Id:D}/robot-authoring-imports");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [IntegrationFact]
    public async Task RawLuaZip_UsesTheAuthoringImportLifecycleWithoutTechnicalContracts()
    {
        var actorId = Guid.NewGuid();
        var organization = Organization("RAW-LUA-IMPORT");
        await using (var seed = fixture.CreateDbContext())
        {
            seed.Add(new Account
            {
                Id = actorId,
                UserName = $"raw-lua-{Guid.NewGuid():N}",
                Email = $"raw-lua-{Guid.NewGuid():N}@example.test",
                Status = AccountStatus.Active
            });
            seed.Add(organization);
            await seed.SaveChangesAsync();
        }

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            fixture.CreateObjectStorage(autoCreateBucket: true),
            actorId,
            "OrgAdmin",
            [$"OrgAdmin|{organization.Id:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();
        using var content = new MultipartFormDataContent();
        var zipContent = new ByteArrayContent(CreateRawLuaZip());
        zipContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(zipContent, "bundle", "real-demo-1408.zip");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        using var response = await client.PostAsync(
            $"/api/v1/management/organizations/{organization.Id:D}/robot-authoring-imports", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("Materialized", data.GetProperty("status").GetString());
        Assert.Equal("REAL-DEMO-1408", data.GetProperty("proposedProgramCode").GetString());
        Assert.Equal("FAIRINO_LUA_V1", data.GetProperty("runtimeTargetCode").GetString());
        Assert.Equal("FR5", data.GetProperty("machineModelCode").GetString());
        Assert.All(data.GetProperty("items").EnumerateArray(), item =>
            Assert.Equal(JsonValueKind.Null, item.GetProperty("technicalContractId").ValueKind));

        await using var assertion = fixture.CreateDbContext();
        var importId = data.GetProperty("id").GetGuid();
        var import = await assertion.RobotAuthoringImports.FindAsync(importId);
        Assert.NotNull(import);
        Assert.Empty(assertion.RobotArtifactTechnicalContracts.Where(contract => contract.OrganizationId == organization.Id));
    }

    private static byte[] CreateRawLuaZip()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var fileName in new[] { "real-demo-1408_step1.lua", "real-demo-1408_step2.lua" })
            {
                var entry = archive.CreateEntry(fileName);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
                writer.Write("-- raw Lua\nreturn 0");
            }
        }

        return output.ToArray();
    }

    private static Organization Organization(string prefix) => new()
    {
        Code = $"{prefix}-{Guid.NewGuid():N}"[..30],
        Name = "Robot import API organization",
        Status = EntityStatus.Active
    };

    private static RobotAuthoringImport Import(Guid organizationId, Guid actorId, string code)
    {
        var import = RobotAuthoringImport.Create(
            organizationId,
            null,
            null,
            null,
            Guid.NewGuid(),
            new string('a', 64),
            Guid.NewGuid().ToString("N"),
            1,
            code,
            code,
            "FAIRINO_LUA_V1",
            "FR5",
            $"robot-authoring-imports/{Guid.NewGuid():N}.zip",
            actorId);
        import.AddItem("PREPARE", "prepare.lua", "prepare.icebot.json", 1,
            new string('b', 64), new string('c', 64));
        return import;
    }
}
