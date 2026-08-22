using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IceBot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.ProductionPackages;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ProductionPackageInstallationApiIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task PackageInstallationHttpFlow_PreviewsInstallsReadsWorkspace_AndRetriesIdempotently()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var basePath = $"/api/v1/management/organizations/{scenario.OrganizationId:D}/production-package-installations";
        var requestBody = new
        {
            scenario.PackageId,
            PackageVersionId = scenario.PackageVersionId,
            scenario.StoreId,
            scenario.KioskId,
            ProductSourceKeys = new[] { scenario.ProductSourceKey }
        };

        using (var previewResponse = await client.PostAsJsonAsync($"{basePath}/preview", requestBody))
        {
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
            using var preview = await ReadJsonAsync(previewResponse);
            Assert.True(preview.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Equal(scenario.PackageVersionId,
                preview.RootElement.GetProperty("data").GetProperty("packageVersionId").GetGuid());
        }

        var idempotencyKey = $"http-install-{Guid.NewGuid():N}";
        Guid installationId;
        Guid productId;
        Guid variantId;
        Guid recipeId;
        Guid releaseId;
        using (var installResponse = await SendInstallAsync(client, basePath, idempotencyKey, requestBody))
        {
            Assert.Equal(HttpStatusCode.Created, installResponse.StatusCode);
            using var installed = await ReadJsonAsync(installResponse);
            var data = installed.RootElement.GetProperty("data");
            Assert.True(installed.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Equal("Installed", data.GetProperty("status").GetString());
            installationId = data.GetProperty("id").GetGuid();
        }

        using (var getResponse = await client.GetAsync($"{basePath}/{installationId:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            using var installation = await ReadJsonAsync(getResponse);
            Assert.Equal(installationId,
                installation.RootElement.GetProperty("data").GetProperty("id").GetGuid());
        }

        using (var workspaceResponse = await client.GetAsync($"{basePath}/{installationId:D}/workspace"))
        {
            Assert.Equal(HttpStatusCode.OK, workspaceResponse.StatusCode);
            using var workspace = await ReadJsonAsync(workspaceResponse);
            var data = workspace.RootElement.GetProperty("data");
            Assert.True(workspace.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Equal(installationId, data.GetProperty("installationId").GetGuid());
            productId = data.GetProperty("products")[0].GetProperty("id").GetGuid();
            variantId = data.GetProperty("productVariants")[0].GetProperty("id").GetGuid();
            recipeId = data.GetProperty("recipes")[0].GetProperty("id").GetGuid();
            releaseId = data.GetProperty("release").GetProperty("id").GetGuid();
        }

        await AssertSuccessfulAsync(await client.PatchAsJsonAsync(
            $"/api/v1/management/organizations/{scenario.OrganizationId:D}/products/{productId:D}/variants/{variantId:D}/recipes/{recipeId:D}/status",
            new { Status = "Published" }));
        await AssertSuccessfulAsync(await client.PatchAsync(
            $"/api/v1/management/organizations/{scenario.OrganizationId:D}/configuration-releases/{releaseId:D}/publish",
            null));

        string deploymentChecksum;
        using (var previewDeployment = await client.PostAsJsonAsync(
                   $"/api/v1/management/kiosks/{scenario.KioskId:D}/configuration-deployments/preview",
                   new
                   {
                       ConfigurationReleaseId = releaseId,
                       KioskExecutionEndpointId = scenario.ExecutionEndpointId,
                       Selections = Array.Empty<object>()
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, previewDeployment.StatusCode);
            using var preview = await ReadJsonAsync(previewDeployment);
            var endpoint = preview.RootElement.GetProperty("data").GetProperty("endpoints")[0];
            Assert.True(endpoint.GetProperty("isEligible").GetBoolean(), endpoint.ToString());
            deploymentChecksum = endpoint.GetProperty("deploymentChecksum").GetString()!;
        }

        var deployPath =
            $"/api/v1/management/kiosks/{scenario.KioskId:D}/configuration-deployments/full-edge";
        var deployBody = new
        {
            ConfigurationReleaseId = releaseId,
            KioskExecutionEndpointId = scenario.ExecutionEndpointId,
            DeploymentPreviewChecksum = deploymentChecksum,
            Reason = "Install published package",
            AcknowledgeRemainingRisk = true
        };
        using (var stalePreviewResponse = await SendWithIdempotencyKeyAsync(
                   client,
                   deployPath,
                   $"stale-deploy-{Guid.NewGuid():N}",
                   new
                   {
                       ConfigurationReleaseId = releaseId,
                       KioskExecutionEndpointId = scenario.ExecutionEndpointId,
                       DeploymentPreviewChecksum = new string('0', 64),
                       Reason = "Verify stale deployment preview",
                       AcknowledgeRemainingRisk = true
                   }))
        {
            Assert.Equal(HttpStatusCode.Conflict, stalePreviewResponse.StatusCode);
            using var stalePreview = await ReadJsonAsync(stalePreviewResponse);
            Assert.False(stalePreview.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Contains("preview", stalePreview.RootElement.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase);
        }
        await using (var rejectedDeploymentContext = fixture.CreateDbContext())
        {
            Assert.Equal(0, await rejectedDeploymentContext.KioskConfigurationDeployments.CountAsync(
                deployment => deployment.ConfigurationReleaseId == releaseId));
        }

        var deployIdempotencyKey = $"deploy-{Guid.NewGuid():N}";
        Guid deploymentId;
        using (var deployResponse = await SendWithIdempotencyKeyAsync(
                   client, deployPath, deployIdempotencyKey, deployBody))
        {
            Assert.Equal(HttpStatusCode.Created, deployResponse.StatusCode);
            using var deployment = await ReadJsonAsync(deployResponse);
            Assert.True(deployment.RootElement.GetProperty("succeeded").GetBoolean());
            deploymentId = deployment.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        }
        using (var retryDeployment = await SendWithIdempotencyKeyAsync(
                   client, deployPath, deployIdempotencyKey, deployBody))
        {
            Assert.Equal(HttpStatusCode.OK, retryDeployment.StatusCode);
            using var deployment = await ReadJsonAsync(retryDeployment);
            Assert.Equal(deploymentId,
                deployment.RootElement.GetProperty("data").GetProperty("id").GetGuid());
        }

        using (var retryResponse = await SendInstallAsync(client, basePath, idempotencyKey, requestBody))
        {
            Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
            using var retry = await ReadJsonAsync(retryResponse);
            Assert.Equal(installationId,
                retry.RootElement.GetProperty("data").GetProperty("id").GetGuid());
        }
        using (var mismatchedRetryResponse = await SendInstallAsync(
                   client,
                   basePath,
                   idempotencyKey,
                   new
                   {
                       scenario.PackageId,
                       PackageVersionId = scenario.PackageVersionId,
                       scenario.StoreId,
                       KioskId = (Guid?)null,
                       ProductSourceKeys = new[] { scenario.ProductSourceKey }
                   }))
        {
            Assert.Equal(HttpStatusCode.Conflict, mismatchedRetryResponse.StatusCode);
            using var mismatch = await ReadJsonAsync(mismatchedRetryResponse);
            Assert.False(mismatch.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Contains("different installation payload",
                mismatch.RootElement.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase);
        }

        await using (var assertionContext = fixture.CreateDbContext())
        {
            Assert.Equal(1, await assertionContext.KioskConfigurationDeployments.CountAsync(
                deployment => deployment.Id == deploymentId));
            Assert.Equal(1, await assertionContext.EdgeCommands.CountAsync(
                command => command.DeploymentId == deploymentId));
        }
    }

    [IntegrationFact]
    public async Task PackageInstallationHttpFlow_RejectsUnknownProductWithoutCreatingInstallation()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();

        using var response = await SendInstallAsync(
            client,
            $"/api/v1/management/organizations/{scenario.OrganizationId:D}/production-package-installations",
            $"invalid-selection-{Guid.NewGuid():N}",
            new
            {
                scenario.PackageId,
                PackageVersionId = scenario.PackageVersionId,
                scenario.StoreId,
                scenario.KioskId,
                ProductSourceKeys = new[] { "UNKNOWN_PRODUCT" }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var error = await ReadJsonAsync(response);
        Assert.False(error.RootElement.GetProperty("succeeded").GetBoolean());
        await using var assertionContext = fixture.CreateDbContext();
        Assert.Equal(0, await assertionContext.ProductionPackageInstallations.CountAsync(
            installation => installation.OrganizationId == scenario.OrganizationId));
    }

    [IntegrationFact]
    public async Task PackageInstallationHttpFlow_RejectsManagerOutsideOrganizationScope()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        var unrelatedOrganizationId = Guid.NewGuid();
        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            storage,
            actorId,
            "Manager",
            [$"Manager|{unrelatedOrganizationId:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/management/organizations/{scenario.OrganizationId:D}/production-package-installations/preview",
            new
            {
                scenario.PackageId,
                PackageVersionId = scenario.PackageVersionId,
                scenario.StoreId,
                scenario.KioskId,
                ProductSourceKeys = new[] { scenario.ProductSourceKey }
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> SendInstallAsync(
        HttpClient client,
        string path,
        string idempotencyKey,
        object body)
    {
        return SendWithIdempotencyKeyAsync(client, path, idempotencyKey, body);
    }

    private static Task<HttpResponseMessage> SendWithIdempotencyKeyAsync(
        HttpClient client,
        string path,
        string idempotencyKey,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static async Task AssertSuccessfulAsync(HttpResponseMessage response)
    {
        using (response)
        {
            using var document = await ReadJsonAsync(response);
            Assert.True(response.IsSuccessStatusCode, document.RootElement.ToString());
            Assert.True(document.RootElement.GetProperty("succeeded").GetBoolean(),
                document.RootElement.ToString());
        }
    }
}
