using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;

namespace IceBot.IntegrationTests.Tenants;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class FranchiseOnboardingApiIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task StartListGetAndRetry_UseOneReadyWorkflow()
    {
        var actorId = Guid.NewGuid();
        var organization = new Organization
        {
            Code = $"ONBOARD-API-{Guid.NewGuid():N}",
            Name = "Onboarding API organization",
            Status = EntityStatus.Active
        };
        await using (var seed = fixture.CreateDbContext())
        {
            seed.AddRange(organization, new Account
            {
                Id = actorId,
                UserName = $"onboarding-{Guid.NewGuid():N}",
                Email = $"onboarding-{Guid.NewGuid():N}@example.test",
                Status = AccountStatus.Active
            });
            await seed.SaveChangesAsync();
        }

        await using var factory = new PackageApiWebApplicationFactory(
            fixture, fixture.CreateObjectStorage(autoCreateBucket: true), actorId);
        using var client = factory.CreateAuthenticatedClient();
        var path = $"/api/v1/management/organizations/{organization.Id:D}/franchise-onboardings";
        var payload = new
        {
            Store = new { Code = $"STORE-{Guid.NewGuid():N}"[..30], Name = "New store", TimeZone = "Asia/Bangkok" },
            Kiosk = new { Code = $"KIOSK-{Guid.NewGuid():N}"[..30], Name = "New kiosk", TimeZone = "Asia/Bangkok" },
            ProductSourceKeys = Array.Empty<string>()
        };

        var key = $"onboard-{Guid.NewGuid():N}";
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", key);
        using var first = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var firstJson = await JsonDocument.ParseAsync(await first.Content.ReadAsStreamAsync());
        var onboardingId = firstJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal("ReadyForActivation",
            firstJson.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var retryRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        retryRequest.Headers.Add("Idempotency-Key", key);
        using var retry = await client.SendAsync(retryRequest);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

        using var list = await client.GetAsync($"{path}?status=ReadyForActivation");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
        Assert.Equal(1, listJson.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());

        using var get = await client.GetAsync($"{path}/{onboardingId:D}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }
}
