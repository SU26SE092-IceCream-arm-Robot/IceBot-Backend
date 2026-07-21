using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Orders.Entities;
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
            code.GetString() is "403" or "404");

        using var fulfillmentResponse = await client.PostAsJsonAsync(
            $"/api/v1/management/orders/{foreignOrderId:D}/items/{Guid.NewGuid():D}/manual-fulfillment-events",
            new
            {
                fulfillmentEventId = Guid.NewGuid(),
                eventType = "Accepted"
            });
        Assert.Equal(HttpStatusCode.Forbidden, fulfillmentResponse.StatusCode);

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
            OrderNumber = $"TENANCY-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
