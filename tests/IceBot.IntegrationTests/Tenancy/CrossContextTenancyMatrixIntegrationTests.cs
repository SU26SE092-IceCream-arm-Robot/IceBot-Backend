using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionPackages;
using IceBot.IntegrationTests.Infrastructure;
using IceBot.IntegrationTests.ProductionPackages;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Tenancy;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class CrossContextTenancyMatrixIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task StrongRole_CannotBorrowForeignScopeFromRoleThatDoesNotAuthorizeOperation()
    {
        var actorId = Guid.NewGuid();
        var allowedOrganizationId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var foreign = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            storage,
            actorId,
            "OrgAdmin",
            [
                $"OrgAdmin|{allowedOrganizationId:D}|*|*",
                $"Manager|{foreign.OrganizationId:D}|*|*"
            ]);
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [IntegrationFact]
    public async Task Manager_CannotMutateGlobalPaymentMethodCatalog()
    {
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            storage,
            actorId,
            "Manager",
            [$"Manager|{organizationId:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/management/payment-methods/1/status",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [IntegrationFact]
    public async Task OrganizationScopedActor_CannotReadAnotherTenantAcrossManagementSurfaces()
    {
        var actorId = Guid.NewGuid();
        var allowedOrganizationId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var foreign = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        var foreignOrderId = await SeedOrderAsync(foreign);
        var foreignInstallationId = await SeedInstallationAsync(foreign);

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            storage,
            actorId,
            "OrgAdmin",
            [$"OrgAdmin|{allowedOrganizationId:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();

        var forbiddenPaths = new[]
        {
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/production-package-installations",
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/configuration-releases",
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/robot-artifacts",
            $"/api/v1/management/kiosks/{foreign.KioskId:D}/inventory/topology"
        };

        foreach (var path in forbiddenPaths)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        using var orderResponse = await client.PostAsJsonAsync("/graphql", new
        {
            query = "query($id: UUID!) { order(id: $id) { id orderNumber } }",
            variables = new { id = foreignOrderId }
        });
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        using var orderDocument = await ReadJsonAsync(orderResponse);
        var errors = orderDocument.RootElement.GetProperty("errors");
        Assert.NotEqual(0, errors.GetArrayLength());
        Assert.Contains(errors.EnumerateArray(), error =>
            error.TryGetProperty("extensions", out var extensions) &&
            extensions.TryGetProperty("code", out var code) &&
            code.GetString() is "403" or "404" or "AUTH_NOT_AUTHORIZED");

        using var fulfillmentResponse = await client.PostAsJsonAsync(
            $"/api/v1/management/orders/{foreignOrderId:D}/items/{Guid.NewGuid():D}/manual-fulfillment-events",
            new
            {
                fulfillmentEventId = Guid.NewGuid(),
                eventType = "Accepted"
            });
        Assert.Equal(HttpStatusCode.Forbidden, fulfillmentResponse.StatusCode);

        using var repairResponse = await client.PostAsync(
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/production-package-installations/" +
            $"{foreignInstallationId:D}/repair",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, repairResponse.StatusCode);

        using var upgradePreviewResponse = await client.PostAsJsonAsync(
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/production-package-installations/" +
            $"{foreignInstallationId:D}/upgrades/preview",
            new
            {
                targetPackageVersionId = Guid.NewGuid(),
                productSourceKeys = Array.Empty<string>()
            });
        Assert.Equal(HttpStatusCode.Forbidden, upgradePreviewResponse.StatusCode);

        using var upgradeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/management/organizations/{foreign.OrganizationId:D}/production-package-installations/" +
            $"{foreignInstallationId:D}/upgrades")
        {
            Content = JsonContent.Create(new
            {
                targetPackageVersionId = Guid.NewGuid(),
                previewChecksum = new string('c', 64),
                productSourceKeys = Array.Empty<string>()
            })
        };
        upgradeRequest.Headers.Add("Idempotency-Key", $"foreign-upgrade-{Guid.NewGuid():N}");
        using var upgradeResponse = await client.SendAsync(upgradeRequest);
        Assert.Equal(HttpStatusCode.Forbidden, upgradeResponse.StatusCode);

        await using var assertionContext = fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.Orders.CountAsync(order => order.Id == foreignOrderId));
    }

    private async Task<Guid> SeedOrderAsync(ProductionPackageInstallationScenario scenario)
    {
        await using var dbContext = fixture.CreateDbContext();
        var order = new Order
        {
            OrganizationId = scenario.OrganizationId,
            StoreId = scenario.StoreId,
            KioskId = scenario.KioskId,
            Channel = OrderChannel.Admin,
            OrderNumber = $"TENANCY-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    private async Task<Guid> SeedInstallationAsync(ProductionPackageInstallationScenario scenario)
    {
        await using var dbContext = fixture.CreateDbContext();
        var installation = ProductionPackageInstallation.Start(
            scenario.OrganizationId,
            scenario.StoreId,
            scenario.KioskId,
            scenario.PackageVersionId,
            new string('a', 64),
            new string('b', 64),
            $"tenancy-{Guid.NewGuid():N}",
            [scenario.ProductSourceKey],
            DateTimeOffset.UtcNow.AddMinutes(-1));
        installation.Fail("TENANCY_TEST", "Seeded foreign installation.", DateTimeOffset.UtcNow);
        dbContext.ProductionPackageInstallations.Add(installation);
        await dbContext.SaveChangesAsync();
        return installation.Id;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
