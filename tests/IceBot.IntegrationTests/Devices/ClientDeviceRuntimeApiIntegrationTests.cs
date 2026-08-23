using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.ClientDevices.Security;
using Domain.Common.Enums;
using Domain.Devices.ClientDevices;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IceBot.IntegrationTests.Devices;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ClientDeviceRuntimeApiIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task Client_device_session_rejects_a_missing_or_mismatched_device_header()
    {
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, Guid.NewGuid());
        using var client = factory.CreateClient();
        var clientDeviceId = Guid.NewGuid();
        var requestBody = new
        {
            clientDeviceId,
            installationId = Guid.NewGuid(),
            credential = Convert.ToBase64String(new byte[32]),
            appVersion = "1.0.0",
            platform = "test"
        };

        using (var missingHeader = await client.PostAsJsonAsync("/api/v1/client-device-sessions", requestBody))
        {
            Assert.Equal(HttpStatusCode.BadRequest, missingHeader.StatusCode);
        }

        using var mismatchedHeaderRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/client-device-sessions")
        {
            Content = JsonContent.Create(requestBody)
        };
        mismatchedHeaderRequest.Headers.Add("X-Client-Device-Id", Guid.NewGuid().ToString());
        using var mismatchedHeader = await client.SendAsync(mismatchedHeaderRequest);

        Assert.Equal(HttpStatusCode.BadRequest, mismatchedHeader.StatusCode);
    }

    [IntegrationFact]
    public async Task Runtime_surface_rejects_account_authentication_and_a_device_token_after_disable()
    {
        var device = await SeedActiveDeviceAsync();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, Guid.NewGuid());
        var tokenIssuer = factory.Services.GetRequiredService<IClientDeviceTokenIssuer>();
        var deviceToken = tokenIssuer.Issue(
            device.Id,
            device.KioskId,
            device.CredentialVersion,
            device.SessionVersion);

        using (var accountClient = factory.CreateAuthenticatedClient())
        using (var accountResponse = await accountClient.GetAsync("/api/v1/runtime/menu"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, accountResponse.StatusCode);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var persisted = await db.ClientDevices.FindAsync(device.Id);
            Assert.NotNull(persisted);
            persisted!.Disable(DateTimeOffset.UtcNow, Guid.NewGuid());
            await db.SaveChangesAsync();
        }

        using var runtimeClient = factory.CreateClient();
        runtimeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceToken);
        using var response = await runtimeClient.GetAsync("/api/v1/runtime/menu");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [IntegrationFact]
    public async Task Runtime_order_rejects_oversized_client_input_and_enforces_the_device_rate_limit()
    {
        var device = await SeedActiveDeviceAsync();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, Guid.NewGuid());
        var tokenIssuer = factory.Services.GetRequiredService<IClientDeviceTokenIssuer>();
        var token = tokenIssuer.Issue(device.Id, device.KioskId, device.CredentialVersion, device.SessionVersion);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var oversized = JsonContent.Create(new
        {
            clientOrderId = new string('x', 101),
            items = new[] { new { menuItemId = Guid.NewGuid(), quantity = 1 } }
        }))
        using (var oversizedResponse = await client.PostAsync("/api/v1/runtime/orders", oversized))
        {
            Assert.Equal(HttpStatusCode.BadRequest, oversizedResponse.StatusCode);
        }

        HttpResponseMessage? lastResponse = null;
        for (var index = 0; index < 13; index++)
        {
            lastResponse?.Dispose();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/runtime/orders")
            {
                Content = JsonContent.Create(new { items = Array.Empty<object>() })
            };
            request.Headers.Add("Idempotency-Key", $"client-device-rate-{index}");
            lastResponse = await client.SendAsync(request);
        }

        using (lastResponse)
        {
            Assert.NotNull(lastResponse);
            Assert.Equal((HttpStatusCode)429, lastResponse!.StatusCode);
        }
    }

    private async Task<ClientDevice> SeedActiveDeviceAsync()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"CLIENT-RUNTIME-{Guid.NewGuid():N}",
            Name = "Client-device runtime organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-RUNTIME-{Guid.NewGuid():N}",
            Name = "Client-device runtime store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-RUNTIME-{Guid.NewGuid():N}",
            Name = "Client-device runtime kiosk",
            Status = KioskStatus.Active
        };
        db.AddRange(organization, store, kiosk);
        await db.SaveChangesAsync();

        var device = ClientDevice.Provision(
            kiosk,
            ClientDeviceType.SelfOrderTablet,
            Guid.NewGuid(),
            "Runtime test tablet",
            "1.0.0",
            "test",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
        device.AddInitialCredential(new byte[32], "test", DateTimeOffset.UtcNow, Guid.NewGuid());
        db.ClientDevices.Add(device);
        await db.SaveChangesAsync();
        return device;
    }
}
