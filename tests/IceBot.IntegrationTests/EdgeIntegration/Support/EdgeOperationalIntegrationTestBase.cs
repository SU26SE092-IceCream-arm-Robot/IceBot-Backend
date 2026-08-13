using Application.RobotConfiguration.Programs.Commands;
using Infrastructure.Concurrency;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Sync.Ingestion;
using Domain.Devices.Telemetry;
using Domain.Devices.Connectivity;
using Domain.Devices.ExecutionEndpoints;
using System.Text;
using System.Text.Json;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.Devices.Telemetry;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Operations.Alerts.Notifications;
using Application.Identity.Tokens.Claims;
using Application.Orders.Management.Queries;
using Application.Orders.Management.Commands;
using Application.Orders.PlaceOrder.Queries;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Bindings;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionPackages.Ownership;
using Application.Inventory.Services;
using Application.Inventory.Commands;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Storage.Services;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Orders.Incidents;
using Domain.Operations.Enums;
using Domain.Operations.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionExecution.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Sync.Enums;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.Devices.Catalog.Persistence;
using Infrastructure.Devices.Connectivity.Persistence;
using Infrastructure.Devices.ExecutionEndpoints.Persistence;
using Infrastructure.Devices.Telemetry.Persistence;
using Infrastructure.Orders.Persistence;
using Infrastructure.Inventory.Persistence;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Infrastructure.ProductionConfiguration.Persistence.Releases;
using Infrastructure.ProductionConfiguration.Persistence.Routes;
using Infrastructure.ProductionConfiguration.Persistence.Bindings;
using Infrastructure.ProductionPackages;
using Infrastructure.RobotConfiguration.Artifacts.Persistence;
using Infrastructure.RobotConfiguration.ArtifactContracts;
using Infrastructure.RobotConfiguration.Programs.Persistence;
using Infrastructure.Persistence.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.RobotConfiguration.ArtifactContracts;

namespace IceBot.IntegrationTests.EdgeIntegration;


public abstract class EdgeOperationalIntegrationTestBase
{
    protected const string RuntimeTargetCode = "FAIRINO_LUA_V1";
    protected const string MachineModelCode = "FR5";
    protected readonly IntegrationTestFixture _fixture;

    protected EdgeOperationalIntegrationTestBase(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    protected async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>> RedispatchAsync(
        Guid orderId,
        CurrentUserContext user,
        string reason,
        int maxDispatchAttempts = 3)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var options = Options.Create(new OrderExecutionDispatchOptions
        {
            MaxDispatchAttempts = maxDispatchAttempts
        });
        var handler = new RedispatchOrderExecutionCommandHandler(
            new OrderStore(dbContext),
            new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dbContext),
                options,
                new NoOpEdgeCommandWakeUpPublisher()),
            new NoOpRealtimeNotificationPublisher());
        return await handler.HandleAsync(new RedispatchOrderExecutionCommand
        {
            OrderId = orderId,
            UserContext = user,
            Reason = reason
        });
    }

    private protected async Task<NoOpRealtimeNotificationPublisher> ReconcileTimeoutAsync(
        SmokeGraph graph,
        Guid commandId,
        DateTimeOffset observedAt)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new OrderExecutionTimeoutStore(dbContext);
        var candidates = await store.ListCandidateCommandIdsAsync(
            observedAt,
            observedAt.AddMinutes(-5),
            observedAt.AddMinutes(-30),
            100);
        Assert.Contains(commandId, candidates);
        var publisher = new NoOpRealtimeNotificationPublisher();
        var handler = new ReconcileOrderExecutionTimeoutCommandHandler(
            store,
            publisher,
            Options.Create(new OrderExecutionDispatchOptions()));
        await handler.HandleAsync(new ReconcileOrderExecutionTimeoutCommand
        {
            SourceCommandId = commandId,
            ObservedAt = observedAt
        });
        return publisher;
    }

    protected async Task PullAndAcknowledgeAsync(
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

    protected async Task AcknowledgeAsync(
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
                PhysicalOutputMayHaveOccurred = physicalOutputMayHaveOccurred,
                // The smoke harness represents an Edge that persisted the pulled command before accepting it.
                LocalStatePersisted = string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase)
            });
        Assert.True(acknowledged.Succeeded, acknowledged.Message);
    }

    protected async Task<Guid> CreatePaidOrderAsync(SmokeGraph graph, int quantity = 1)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var order = new Order
        {
            OrganizationId = graph.OrganizationId,
            StoreId = graph.StoreId,
            KioskId = graph.KioskId,
            OrderNumber = $"SMOKE-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
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
            Domain.Catalog.Enums.FulfillmentType.MachineProduced,
            quantity,
            1,
            recipeSnapshotJson: JsonSerializer.Serialize(new
            {
                Ingredients = new[] { new { graph.IngredientId } }
            }));
        var now = DateTimeOffset.UtcNow;
        order.Place(now, now.AddMinutes(15));
        order.MarkPaid(order.TotalAmount, now);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    protected async Task AssertInventoryDispatchBlockedAsync(SmokeGraph graph, string failureKind)
    {
        await using var mutationContext = _fixture.CreateDbContext();
        var state = await mutationContext.IngredientDispenserStates
            .Include(item => item.Device)
            .SingleAsync(item => item.Id == graph.DispenserStateId);
        var originalProfile = state.LevelToQuantityProfileJson;
        var originalDeviceStatus = state.Device.Status;
        var originalActive = state.IsActive;
        switch (failureKind)
        {
            case "inactive":
                state.IsActive = false;
                break;
            case "calibration":
                state.LevelToQuantityProfileJson = null;
                break;
            case "device":
                state.Device.SetStatus(DeviceStatus.Offline);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(failureKind));
        }
        await mutationContext.SaveChangesAsync();

        var orderId = await CreatePaidOrderAsync(graph);
        await using (var dispatchContext = _fixture.CreateDbContext())
        {
            var result = await new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dispatchContext),
                Options.Create(new OrderExecutionDispatchOptions()),
                new NoOpEdgeCommandWakeUpPublisher()).HandleAsync(new DispatchOrderExecutionCommand
                {
                    OrderId = orderId,
                    DispatchAttemptNo = 1
                });
            Assert.False(result.Succeeded);
            Assert.Equal(409, result.StatusCode);
        }

        state.IsActive = originalActive;
        state.LevelToQuantityProfileJson = originalProfile;
        state.Device.SetStatus(originalDeviceStatus);
        await mutationContext.SaveChangesAsync();
    }

    protected async Task ReportProductionAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid? productionJobId,
        long sequenceNumber,
        string status,
        Guid releaseId,
        string releaseChecksum,
        IReadOnlyCollection<StockMovementEvidenceInput>? stockMovements = null,
        int productionUnitNo = 1)
    {
        var result = await IngestProductionAsync(
            graph,
            commandId,
            productionJobId,
            sequenceNumber,
            status,
            releaseId,
            releaseChecksum,
            stockMovements,
            productionUnitNo);
        Assert.True(result.Succeeded, result.Message);
    }

    protected async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.Reports.Results.ExecutionReportIngestResult>>
        IngestProductionAsync(
            SmokeGraph graph,
            Guid commandId,
            Guid? productionJobId,
            long sequenceNumber,
            string status,
            Guid releaseId,
            string releaseChecksum,
            IReadOnlyCollection<StockMovementEvidenceInput>? stockMovements = null,
            int productionUnitNo = 1)
    {
        await using var dbContext = _fixture.CreateDbContext();
        Guid? orderItemId = null;
        var normalizedStockMovements = stockMovements ?? [];
        if (productionJobId.HasValue)
        {
            var edgeCommand = await dbContext.EdgeCommands.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == commandId);
            if (!edgeCommand.OrderId.HasValue)
                throw new InvalidOperationException("Production-job test command has no order identity.");
            var orderItem = await dbContext.OrderItems.AsNoTracking()
                .SingleAsync(candidate => candidate.OrderId == edgeCommand.OrderId.Value);
            orderItemId = orderItem.Id;
            normalizedStockMovements = normalizedStockMovements
                .Select(evidence => evidence.OrderItemId == Guid.Empty
                    ? evidence with { OrderItemId = orderItem.Id }
                    : evidence)
                .ToArray();
        }
        var reportStore = new ExecutionReportStore(dbContext);
        return await new IngestExecutionReportCommandHandler(
            reportStore,
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = Guid.NewGuid(),
                SequenceNumber = sequenceNumber,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "ProductionExecution",
                Status = status,
                SourceProductionJobId = productionJobId,
                OrderItemId = orderItemId,
                ProductionUnitNo = productionJobId.HasValue ? productionUnitNo : null,
                ProductionUnitQuantity = productionJobId.HasValue ? 1 : null,
                SourceConfigurationReleaseId = releaseId,
                ReleaseChecksum = releaseChecksum,
                PhysicalOutputMayHaveOccurred = status is "Running" or "Completed",
                StockMovements = normalizedStockMovements
            });
    }

    protected async Task RefillAsync(
        SmokeGraph graph,
        CurrentUserContext user,
        decimal quantity,
        string reasonCode)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new RefillDispenserCommandHandler(
                new InventoryStore(dbContext),
                new NoOpRealtimeNotificationPublisher())
            .HandleAsync(new RefillDispenserCommand
            {
                KioskId = graph.KioskId,
                DispenserStateId = graph.DispenserStateId,
                UserContext = user,
                Quantity = quantity,
                ReasonCode = reasonCode
            });
        Assert.True(result.Succeeded, result.Message);
    }

    protected static void AssertPostgresTimestampEqual(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((expected - actual).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
    }

    protected async Task PullAndAcceptAsync(SmokeGraph graph, Guid commandId)
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
            var pulledCommand = Assert.Single(pulled.Data!.Commands, command => command.CommandId == commandId);
            if (pulledCommand.CommandType == EdgeCommandType.DeployConfiguration.ToString())
            {
                using var payload = JsonDocument.Parse(pulledCommand.PayloadJson);
                var bundle = payload.RootElement.GetProperty("FullEdgeBundle");
                Assert.EndsWith(".zip", bundle.GetProperty("StorageKey").GetString());
                Assert.False(string.IsNullOrWhiteSpace(bundle.GetProperty("DownloadUrl").GetString()));
                Assert.All(payload.RootElement.GetProperty("Artifacts").EnumerateArray(), artifact =>
                    Assert.False(string.IsNullOrWhiteSpace(artifact.GetProperty("DownloadUrl").GetString())));
            }
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
                    AcknowledgedAt = DateTimeOffset.UtcNow,
                    LocalStatePersisted = true
                });
            Assert.True(accepted.Succeeded, accepted.Message);
        }
    }

    protected async Task ReportAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid deploymentId,
        Guid sourceEventId,
        long sequenceNumber,
        string status)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var deployment = await dbContext.KioskConfigurationDeployments
            .AsNoTracking()
            .SingleAsync(x => x.Id == deploymentId);
        var reportStore = new ExecutionReportStore(dbContext);
        var result = await new IngestExecutionReportCommandHandler(
            reportStore,
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
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
                DeploymentId = deploymentId,
                SourceConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseChecksum = deployment.ReleaseChecksum
            });
        Assert.True(result.Succeeded, result.Message);
    }

    protected static Application.Devices.Connectivity.Contracts.LocalPersistenceHealthInput HealthyLocalPersistence() =>
        new(true, 10L * 1024 * 1024 * 1024, 1024L * 1024 * 1024,
            Application.Devices.Connectivity.Contracts.LocalDatabaseHealth.Healthy, 0, 10_000);

    protected async Task AssertDeploymentProvenanceRejectedAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid deploymentId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var deployment = await dbContext.KioskConfigurationDeployments
            .AsNoTracking()
            .SingleAsync(x => x.Id == deploymentId);
        var result = await new IngestExecutionReportCommandHandler(
            new ExecutionReportStore(dbContext),
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = Guid.NewGuid(),
                SequenceNumber = 1,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "Deployment",
                Status = "Installed",
                DeploymentId = deploymentId,
                SourceConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseChecksum = new string('f', 64)
            });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("provenance", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    protected async Task<SmokeGraph> SeedPrerequisitesAsync()
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
        var deviceType = new DeviceType
        {
            Code = $"DISPENSER-{Guid.NewGuid():N}",
            Name = "Operational smoke dispenser"
        };
        dbContext.DeviceTypes.Add(deviceType);
        await dbContext.SaveChangesAsync();

        var device = Device.CreateProvisioning(
            deviceType.Id,
            null,
            kiosk.Id,
            $"DEVICE-{Guid.NewGuid():N}",
            "Operational smoke dispenser",
            null,
            null,
            null,
            null);
        device.DeviceType = deviceType;
        device.Kiosk = kiosk;
        device.SetStatus(DeviceStatus.Online);
        var ingredient = new Ingredient
        {
            Code = $"INGREDIENT-{Guid.NewGuid():N}",
            Name = "Operational smoke ingredient",
            Unit = "gram"
        };
        recipe.RecipeItems.Add(new RecipeItem
        {
            RecipeId = recipe.Id,
            Recipe = recipe,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            Quantity = 10,
            Unit = "gram",
            StepOrder = 1,
            IsOptional = false
        });
        var dispenserState = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = $"CONTAINER-{Guid.NewGuid():N}",
            CurrentLevelStatus = IngredientLevelStatus.Full,
            EstimatedQuantity = 100,
            CapacityQuantity = 100,
            Unit = "gram",
            LevelToQuantityProfileJson =
                """[{"Level":1,"EstimatedQuantity":10},{"Level":2,"EstimatedQuantity":50},{"Level":3,"EstimatedQuantity":100}]""",
            LastMeasuredAt = DateTimeOffset.UtcNow
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

        dbContext.AddRange(
            account,
            organization,
            store,
            kiosk,
            product,
            variant,
            recipe,
            menu,
            menuItem,
            device,
            ingredient,
            dispenserState,
            endpoint);
        await dbContext.SaveChangesAsync();

        var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        endpoint.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointCredentialBindings.Add(credential);
        var readiness = ExecutionEndpointReadinessProjection.Create(
            kiosk.Id, endpoint.Id, endpoint.FullEdgeRuntimeId!.Value, 1,
            ExecutionReadinessState.Ready, ExecutionActivityState.Idle, ExecutionSafetyState.Safe,
            null, PhysicalOutputState.No, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointReadinessProjections.Add(readiness);
        dbContext.ExecutionEndpointCapabilityProjections.Add(new ExecutionEndpointCapabilityProjection
        {
            ExecutionEndpointReadinessProjectionId = readiness.Id,
            CapabilityCode = "ICE_CREAM",
            IsAvailable = true
        });
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
            menuItem.Id,
            device.Id,
            ingredient.Id,
            dispenserState.Id,
            endpoint.FullEdgeRuntimeId!.Value);
    }

    protected sealed record SmokeGraph(
        Guid AccountId,
        Guid OrganizationId,
        Guid StoreId,
        Guid KioskId,
        Guid EndpointId,
        Guid ProductId,
        Guid ProductVariantId,
        Guid RecipeId,
        Guid MenuItemId,
        Guid DeviceId,
        Guid IngredientId,
        Guid DispenserStateId,
        Guid SourceExecutorId);

    protected async Task<ActiveRuntimeGraph> CreateActiveRuntimeAsync()
    {
        var graph = await SeedPrerequisitesAsync();
        var user = new CurrentUserContext { AccountId = graph.AccountId, IsSystemAdmin = true };
        var luaBytes = Encoding.UTF8.GetBytes("print('operational-smoke')");

        Guid artifactId;
        Guid programId;
        Guid productionProgramBindingId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var robotArtifactStore = new RobotArtifactStore(dbContext);
            var robotProgramStore = new RobotProgramStore(dbContext);
            var mutationCoordinator = new PostgresTechnicalResourceMutationCoordinator(dbContext);
            var packageOwnership = new ProductionPackageTechnicalOwnershipPolicy(
                new ProductionPackageTechnicalOwnershipStore(dbContext));
            var technicalContractStore = new RobotArtifactTechnicalContractStore(dbContext);
            var technicalContract = RobotArtifactTechnicalContract.CreateDraft(
                $"SMOKE-{Guid.NewGuid():N}", 1, RuntimeTargetCode, MachineModelCode, graph.OrganizationId);
            technicalContract.ReplaceDefinition(
                [new RobotArtifactEffectDefinition("MAKE_ICE_CREAM", RobotArtifactEffectKind.System, null, null,
                    RobotArtifactQuantityMode.None, null, null, null)],
                []);
            technicalContract.Publish(DateTimeOffset.UtcNow, user.AccountId, parameterizedRuntimeSupported: false);
            await technicalContractStore.AddAsync(technicalContract, CancellationToken.None);
            var objectStorage = _fixture.CreateObjectStorage(autoCreateBucket: true);
            var upload = new UploadRobotArtifactCommandHandler(
                robotArtifactStore,
                new ArtifactUploadContentService(
                    objectStorage,
                    NullLogger<ArtifactUploadContentService>.Instance),
                mutationCoordinator,
                technicalContractStore);
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
                        MachineModelCode = MachineModelCode,
                        TechnicalContractId = technicalContract.Id
                    }
                ]
            });
            Assert.True(uploaded.Succeeded, uploaded.Message);
            artifactId = Assert.Single(uploaded.Data!.Items).RobotArtifactId!.Value;

            var publishedArtifact = await new PublishRobotArtifactCommandHandler(
                robotArtifactStore,
                new ArtifactPublicationValidator(technicalContractStore, objectStorage),
                mutationCoordinator).HandleAsync(
                new PublishRobotArtifactCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ArtifactId = artifactId
                });
            Assert.True(publishedArtifact.Succeeded, publishedArtifact.Message);

            var createdProgram = await new CreateRobotProgramCommandHandler(
                robotProgramStore, mutationCoordinator).HandleAsync(
                new CreateRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    Code = $"SMOKE-{Guid.NewGuid():N}",
                    Name = "Operational smoke program"
                });
            Assert.True(createdProgram.Succeeded, createdProgram.Message);
            programId = createdProgram.Data!.Id;

            var assigned = await new ReplaceRobotProgramArtifactsCommandHandler(
                robotProgramStore, robotArtifactStore, packageOwnership, mutationCoordinator).HandleAsync(
                new ReplaceRobotProgramArtifactsCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId,
                    Artifacts = [new RobotProgramArtifactInput(artifactId, 1, 1, null)]
                });
            Assert.True(assigned.Succeeded, assigned.Message);

            var publishedProgram = await new PublishRobotProgramCommandHandler(
                robotProgramStore, robotArtifactStore, mutationCoordinator).HandleAsync(
                new PublishRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId
                });
            Assert.True(publishedProgram.Succeeded, publishedProgram.Message);

            var productionBinding = await new ProductionProgramBindingHandlers(
                new ProductionProgramBindingStore(dbContext)).CreateAsync(
                new CreateProductionProgramBindingCommand(
                    user,
                    graph.OrganizationId,
                    graph.RecipeId,
                    programId,
                    Array.Empty<string>()),
                CancellationToken.None);
            Assert.True(productionBinding.Succeeded, productionBinding.Message);
            productionProgramBindingId = productionBinding.Data!.Id;
        }

        Guid releaseId;
        Guid deploymentId;
        Guid commandId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var releaseStore = new ConfigurationReleaseStore(dbContext);
            var routeStore = new ConfigurationRouteStore(dbContext);
            var deploymentStore = new ConfigurationDeploymentStore(dbContext);
            var packageOwnership = new ProductionPackageTechnicalOwnershipPolicy(
                new ProductionPackageTechnicalOwnershipStore(dbContext));
            var mutationCoordinator = new PostgresTechnicalResourceMutationCoordinator(dbContext);
            var createdRelease = await new CreateConfigurationReleaseCommandHandler(releaseStore).HandleAsync(
                new CreateConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId
                });
            Assert.True(createdRelease.Succeeded, createdRelease.Message);
            releaseId = createdRelease.Data!.Id;

            var routed = await new ReplaceConfigurationReleaseRoutesCommandHandler(
                releaseStore, routeStore, packageOwnership, mutationCoordinator).HandleAsync(
                new ReplaceConfigurationReleaseRoutesCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId,
                    ExpectedRevision = createdRelease.Data.Revision,
                    Routes =
                    [
                        new ConfigurationReleaseRouteInput(
                            graph.RecipeId,
                            "DEFAULT",
                            0,
                            [],
                            Array.Empty<string>(),
                            [new ConfigurationReleaseRobotBindingInput(productionProgramBindingId, 1)])
                    ]
                });
            Assert.True(routed.Succeeded, routed.Message);

            var inventoryReadiness = new ProductionInventoryReadinessGuard(
                new InventoryReadinessEvaluator(new InventoryStore(dbContext)),
                Options.Create(new InventoryReadinessPolicyOptions
                {
                    PublishPolicy = InventoryReadinessPolicy.Warn,
                    DeployPolicy = InventoryReadinessPolicy.Warn
                }));

            var publishedRelease = await new PublishConfigurationReleaseCommandHandler(
                releaseStore,
                inventoryReadiness,
                new ProductionDefinitionPublicationService()).HandleAsync(
                new PublishConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId
                });
            Assert.True(publishedRelease.Succeeded, publishedRelease.Message);

            var edgeStore = new EdgeCommandStore(dbContext);
            var deploymentWakeUpPublisher = new NoOpEdgeCommandWakeUpPublisher { PublishResult = false };
            var deployed = await new DeployFullEdgeConfigurationCommandHandler(
                deploymentStore,
                releaseStore,
                edgeStore,
                deploymentWakeUpPublisher,
                inventoryReadiness,
                new FullEdgeReleaseBundleService(_fixture.CreateObjectStorage(autoCreateBucket: true))).HandleAsync(
                new DeployFullEdgeConfigurationCommand
                {
                    UserContext = user,
                    KioskId = graph.KioskId,
                    ConfigurationReleaseId = releaseId,
                    KioskExecutionEndpointId = graph.EndpointId,
                    IdempotencyKey = Guid.NewGuid().ToString("N"),
                    Reason = "Edge operational integration deployment"
                });
            Assert.True(deployed.Succeeded, deployed.Message);
            deploymentId = deployed.Data!.Id;
            commandId = deployed.Data.EdgeCommandId!.Value;
            var deploymentWakeUp = Assert.Single(deploymentWakeUpPublisher.Notifications);
            Assert.Equal(commandId, deploymentWakeUp.CommandId);
            Assert.Equal(EdgeCommandType.DeployConfiguration, deploymentWakeUp.CommandType);
        }

        await PullAndAcceptAsync(graph, commandId);
        await AssertDeploymentProvenanceRejectedAsync(graph, commandId, deploymentId);
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 1, "Installed");
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 2, "Active");

        await using var assertionContext = _fixture.CreateDbContext();
        var deployment = await assertionContext.KioskConfigurationDeployments.SingleAsync(x => x.Id == deploymentId);
        var endpoint = await assertionContext.KioskExecutionEndpoints.SingleAsync(x => x.Id == graph.EndpointId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Active, deployment.Status);
        Assert.Equal(deploymentId, endpoint.ActiveConfigurationDeploymentId);
        Assert.Equal(releaseId, endpoint.ActiveConfigurationReleaseId);

        return new ActiveRuntimeGraph(
            graph, user, releaseId, deploymentId, commandId, deployment.ReleaseChecksum);
    }

    protected sealed record ActiveRuntimeGraph(
        SmokeGraph Graph,
        CurrentUserContext User,
        Guid ReleaseId,
        Guid DeploymentId,
        Guid DeploymentCommandId,
        string ReleaseChecksum);
}
