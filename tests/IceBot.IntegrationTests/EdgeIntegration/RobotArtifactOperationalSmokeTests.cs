using System.Text;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Commands;
using Application.EdgeIntegration.Services;
using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Commands;
using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Services;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Identity.Entities;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionConfiguration.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.ProductionConfiguration.Persistence;
using Infrastructure.RobotConfiguration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.EdgeIntegration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class RobotArtifactOperationalSmokeTests
{
    private const string RuntimeTargetCode = "FAIRINO_LUA_V1";
    private const string MachineModelCode = "FR5";
    private readonly IntegrationTestFixture _fixture;

    public RobotArtifactOperationalSmokeTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task UploadProgramReleaseDeployAndReportActive_CompletesOperationalFlow()
    {
        var graph = await SeedPrerequisitesAsync();
        var user = new CurrentUserContext { AccountId = graph.AccountId, IsSystemAdmin = true };
        var luaBytes = Encoding.UTF8.GetBytes("print('operational-smoke')");

        Guid artifactId;
        Guid programId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var robotStore = new RobotConfigurationStore(dbContext);
            var upload = new UploadRobotArtifactCommandHandler(
                robotStore,
                new ArtifactUploadContentService(
                    _fixture.CreateObjectStorage(),
                    NullLogger<ArtifactUploadContentService>.Instance));
            var bulkUpload = new BulkUploadRobotArtifactsCommandHandler(upload);
            await using var lua = new MemoryStream(luaBytes);
            var uploaded = await bulkUpload.HandleAsync(new BulkUploadRobotArtifactsCommand
            {
                UserContext = user,
                OrganizationId = graph.OrganizationId,
                Items =
                [
                    new BulkUploadRobotArtifactItem
                    {
                        FileName = "01_make_ice_cream.lua",
                        ContentType = "text/x-lua",
                        ContentLengthBytes = luaBytes.Length,
                        Content = lua,
                        ArtifactCode = $"SMOKE-{Guid.NewGuid():N}",
                        ArtifactName = "Operational smoke artifact",
                        RuntimeTargetCode = RuntimeTargetCode,
                        MachineModelCode = MachineModelCode
                    }
                ]
            });
            Assert.True(uploaded.Succeeded, uploaded.Message);
            artifactId = Assert.Single(uploaded.Data!.Items).RobotArtifactId!.Value;

            var publishedArtifact = await new PublishRobotArtifactCommandHandler(robotStore).HandleAsync(
                new PublishRobotArtifactCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ArtifactId = artifactId
                });
            Assert.True(publishedArtifact.Succeeded, publishedArtifact.Message);

            var createdProgram = await new CreateRobotProgramCommandHandler(robotStore).HandleAsync(
                new CreateRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ScopeType = TenantScopeType.Organization,
                    Code = $"SMOKE-{Guid.NewGuid():N}",
                    Name = "Operational smoke program"
                });
            Assert.True(createdProgram.Succeeded, createdProgram.Message);
            programId = createdProgram.Data!.Id;

            var assigned = await new ReplaceRobotProgramArtifactsCommandHandler(robotStore).HandleAsync(
                new ReplaceRobotProgramArtifactsCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId,
                    Artifacts = [new RobotProgramArtifactInput(artifactId, 1, 1, null)]
                });
            Assert.True(assigned.Succeeded, assigned.Message);

            var publishedProgram = await new PublishRobotProgramCommandHandler(robotStore).HandleAsync(
                new PublishRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId
                });
            Assert.True(publishedProgram.Succeeded, publishedProgram.Message);
        }

        Guid releaseId;
        Guid deploymentId;
        Guid commandId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var productionStore = new ProductionConfigurationStore(dbContext);
            var createdRelease = await new CreateConfigurationReleaseCommandHandler(productionStore).HandleAsync(
                new CreateConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId
                });
            Assert.True(createdRelease.Succeeded, createdRelease.Message);
            releaseId = createdRelease.Data!.Id;

            var routed = await new ReplaceConfigurationReleaseRoutesCommandHandler(productionStore).HandleAsync(
                new ReplaceConfigurationReleaseRoutesCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId,
                    Routes =
                    [
                        new ConfigurationReleaseRouteInput(
                            graph.ProductVariantId,
                            graph.RecipeId,
                            "DEFAULT",
                            0,
                            null,
                            [new ConfigurationReleaseRobotBindingInput(programId, 1, "ICE_CREAM")])
                    ]
                });
            Assert.True(routed.Succeeded, routed.Message);

            var publishedRelease = await new PublishConfigurationReleaseCommandHandler(productionStore).HandleAsync(
                new PublishConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId
                });
            Assert.True(publishedRelease.Succeeded, publishedRelease.Message);

            var edgeStore = new EdgeCommandStore(dbContext);
            var deployed = await new DeployFullEdgeConfigurationCommandHandler(productionStore, edgeStore).HandleAsync(
                new DeployFullEdgeConfigurationCommand
                {
                    UserContext = user,
                    KioskId = graph.KioskId,
                    ConfigurationReleaseId = releaseId,
                    KioskExecutionEndpointId = graph.EndpointId,
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                });
            Assert.True(deployed.Succeeded, deployed.Message);
            deploymentId = deployed.Data!.Id;
            commandId = deployed.Data.EdgeCommandId!.Value;
        }

        await PullAndAcceptAsync(graph, commandId);
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 1, "Installed");
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 2, "Active");

        await using var assertionContext = _fixture.CreateDbContext();
        var deployment = await assertionContext.KioskConfigurationDeployments.SingleAsync(x => x.Id == deploymentId);
        var endpoint = await assertionContext.KioskExecutionEndpoints.SingleAsync(x => x.Id == graph.EndpointId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Active, deployment.Status);
        Assert.Equal(deploymentId, endpoint.ActiveConfigurationDeploymentId);
        Assert.Equal(releaseId, endpoint.ActiveConfigurationReleaseId);

        var orderId = await CreatePaidOrderAsync(graph);
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()));
        var firstDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = orderId,
            DispatchAttemptNo = 1
        });
        Assert.True(firstDispatch.Succeeded, firstDispatch.Message);
        Assert.False(firstDispatch.Data!.Existing);

        var retryDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = orderId,
            DispatchAttemptNo = 1
        });
        Assert.True(retryDispatch.Succeeded, retryDispatch.Message);
        Assert.True(retryDispatch.Data!.Existing);
        Assert.Equal(firstDispatch.Data.EdgeCommandId, retryDispatch.Data.EdgeCommandId);

        var command = await dispatchContext.EdgeCommands.SingleAsync(x => x.OrderId == orderId);
        Assert.Equal(EdgeCommandType.ExecuteOrder, command.CommandType);
        Assert.Equal(graph.EndpointId, command.TargetExecutionEndpointId);
        Assert.Equal(1, command.DispatchAttemptNo);

        await PullAndAcknowledgeAsync(graph, command.Id, "Accepted");
        await using (var acceptedContext = _fixture.CreateDbContext())
        {
            var acceptedOrder = await acceptedContext.Orders.SingleAsync(x => x.Id == orderId);
            Assert.Equal(OrderStatus.Accepted, acceptedOrder.Status);
            Assert.Single(await acceptedContext.OrderStatusHistories
                .Where(x => x.OrderId == orderId && x.ToStatus == OrderStatus.Accepted)
                .ToListAsync());
        }

        var rejectedOrderId = await CreatePaidOrderAsync(graph);
        var rejectedDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = rejectedOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(rejectedDispatch.Succeeded, rejectedDispatch.Message);
        await PullAndAcknowledgeAsync(graph, rejectedDispatch.Data!.EdgeCommandId, "Rejected", false);

        var supportOrderId = await CreatePaidOrderAsync(graph);
        var supportDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = supportOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(supportDispatch.Succeeded, supportDispatch.Message);
        await PullAndAcknowledgeAsync(graph, supportDispatch.Data!.EdgeCommandId, "Rejected", true);

        var busyOrderId = await CreatePaidOrderAsync(graph);
        var busyDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = busyOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(busyDispatch.Succeeded, busyDispatch.Message);
        await PullAndAcknowledgeAsync(graph, busyDispatch.Data!.EdgeCommandId, "ExecutorBusy");
        await AcknowledgeAsync(graph, busyDispatch.Data.EdgeCommandId, "ExecutorBusy");

        await using var ackAssertionContext = _fixture.CreateDbContext();
        Assert.Equal(
            OrderStatus.ExecutionRejected,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == rejectedOrderId)).Status);
        Assert.Equal(
            OrderStatus.RefundRequired,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == supportOrderId)).Status);
        Assert.Equal(
            OrderStatus.ReadyForExecution,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == busyOrderId)).Status);
        var busyCommand = await ackAssertionContext.EdgeCommands
            .Include(x => x.DeliveryAttempts)
            .SingleAsync(x => x.Id == busyDispatch.Data.EdgeCommandId);
        Assert.Equal(EdgeCommandStatus.PendingDelivery, busyCommand.Status);
        Assert.Equal(2, busyCommand.DeliveryAttempts.Count);
    }

    private async Task PullAndAcknowledgeAsync(
        SmokeGraph graph,
        Guid commandId,
        string status,
        bool? physicalOutputMayHaveOccurred = null)
    {
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var pulled = await new PullEdgeCommandsCommandHandler(
                new EdgeCommandStore(dbContext),
                new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()))
                .HandleAsync(new PullEdgeCommandsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    MaxCommands = 10
                });
            Assert.True(pulled.Succeeded, pulled.Message);
            Assert.Contains(pulled.Data!.Commands, item => item.CommandId == commandId);
        }

        await AcknowledgeAsync(graph, commandId, status, physicalOutputMayHaveOccurred);
    }

    private async Task AcknowledgeAsync(
        SmokeGraph graph,
        Guid commandId,
        string status,
        bool? physicalOutputMayHaveOccurred = null)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var acknowledged = await new AcknowledgeEdgeCommandCommandHandler(
            new EdgeCommandStore(dbContext),
            new NoOpRealtimeNotificationPublisher())
            .HandleAsync(new AcknowledgeEdgeCommandCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                AckStatus = status,
                AcknowledgedAt = DateTimeOffset.UtcNow,
                RejectionCode = status == "Rejected" ? "ReadinessRejected" : null,
                PhysicalOutputMayHaveOccurred = physicalOutputMayHaveOccurred
            });
        Assert.True(acknowledged.Succeeded, acknowledged.Message);
    }

    private async Task<Guid> CreatePaidOrderAsync(SmokeGraph graph)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var order = new Order
        {
            OrganizationId = graph.OrganizationId,
            StoreId = graph.StoreId,
            KioskId = graph.KioskId,
            OrderNumber = $"SMOKE-{Guid.NewGuid():N}",
            Currency = "VND"
        };
        order.AddItem(
            graph.MenuItemId,
            graph.ProductId,
            graph.ProductVariantId,
            graph.RecipeId,
            "SMOKE-MENU-ITEM",
            "Operational smoke item",
            "SMOKE-PRODUCT",
            "Operational smoke product",
            "SMOKE-VARIANT",
            "Operational smoke variant",
            1,
            1,
            1);
        var now = DateTimeOffset.UtcNow;
        order.Place(now);
        order.MarkPaid(order.TotalAmount, now);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    private async Task PullAndAcceptAsync(SmokeGraph graph, Guid commandId)
    {
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var pulled = await new PullEdgeCommandsCommandHandler(
                new EdgeCommandStore(dbContext),
                new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()))
                .HandleAsync(new PullEdgeCommandsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    MaxCommands = 10
                });
            Assert.True(pulled.Succeeded, pulled.Message);
            Assert.Contains(pulled.Data!.Commands, command => command.CommandId == commandId);
        }

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var accepted = await new AcknowledgeEdgeCommandCommandHandler(
                new EdgeCommandStore(dbContext),
                new NoOpRealtimeNotificationPublisher())
                .HandleAsync(new AcknowledgeEdgeCommandCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    CommandId = commandId,
                    AckStatus = "Accepted",
                    AcknowledgedAt = DateTimeOffset.UtcNow
                });
            Assert.True(accepted.Succeeded, accepted.Message);
        }
    }

    private async Task ReportAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid deploymentId,
        Guid sourceEventId,
        long sequenceNumber,
        string status)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new IngestExecutionReportCommandHandler(new ExecutionReportStore(dbContext))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = sourceEventId,
                SequenceNumber = sequenceNumber,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "Deployment",
                Status = status,
                DeploymentId = deploymentId
            });
        Assert.True(result.Succeeded, result.Message);
    }

    private async Task<SmokeGraph> SeedPrerequisitesAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var account = new Account
        {
            UserName = $"smoke-{Guid.NewGuid():N}",
            Email = $"smoke-{Guid.NewGuid():N}@example.test",
            Status = Domain.Identity.Enums.AccountStatus.Active
        };
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Operational smoke organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Operational smoke store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Operational smoke kiosk",
            Status = KioskStatus.Active
        };
        var product = new Product
        {
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}",
            Name = "Operational smoke product",
            BasePrice = 1
        };
        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Product = product,
            Code = $"VARIANT-{Guid.NewGuid():N}",
            Name = "Operational smoke variant",
            BasePrice = 1,
            FulfillmentType = FulfillmentType.MachineProduced
        };
        var recipe = new Recipe
        {
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            Code = $"RECIPE-{Guid.NewGuid():N}",
            Name = "Operational smoke recipe",
            Status = RecipeStatus.Published
        };
        var menu = new Menu
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            ScopeType = TenantScopeType.Kiosk,
            Code = $"MENU-{Guid.NewGuid():N}",
            Name = "Operational smoke menu",
            Status = MenuStatus.Active
        };
        var menuItem = new MenuItem
        {
            MenuId = menu.Id,
            Menu = menu,
            ProductId = product.Id,
            Product = product,
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            RecipeId = recipe.Id,
            Recipe = recipe,
            Code = $"ITEM-{Guid.NewGuid():N}",
            DisplayName = "Operational smoke item",
            Status = MenuItemStatus.Active,
            Price = 1
        };
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            $"EDGE-{Guid.NewGuid():N}",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.ReplaceSupportedRobotTargets([(RuntimeTargetCode, MachineModelCode, null)]);

        dbContext.AddRange(account, organization, store, kiosk, product, variant, recipe, menu, menuItem, endpoint);
        await dbContext.SaveChangesAsync();

        var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        endpoint.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointCredentialBindings.Add(credential);
        await dbContext.SaveChangesAsync();

        return new SmokeGraph(
            account.Id,
            organization.Id,
            store.Id,
            kiosk.Id,
            endpoint.Id,
            product.Id,
            variant.Id,
            recipe.Id,
            menuItem.Id);
    }

    private sealed record SmokeGraph(
        Guid AccountId,
        Guid OrganizationId,
        Guid StoreId,
        Guid KioskId,
        Guid EndpointId,
        Guid ProductId,
        Guid ProductVariantId,
        Guid RecipeId,
        Guid MenuItemId);
}
