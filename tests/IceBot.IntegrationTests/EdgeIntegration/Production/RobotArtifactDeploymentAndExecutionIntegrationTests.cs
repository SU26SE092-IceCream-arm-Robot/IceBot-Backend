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
using Application.Orders.PlaceOrder;
using Application.Orders.PlaceOrder.Commands;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Services;
using Application.Orders.Admission;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Requests;
using Application.Orders.PlaceOrder.Queries;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionPackages.Ownership;
using Application.Inventory.Services;
using Application.Inventory.Commands;
using Application.SalesCatalog.Availability;
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
using Infrastructure.Payments.Persistence;
using Infrastructure.SalesCatalog.Persistence;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Infrastructure.ProductionConfiguration.Persistence.Releases;
using Infrastructure.ProductionConfiguration.Persistence.Routes;
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


[Collection(IntegrationTestFixture.CollectionName)]
public sealed class RobotArtifactDeploymentAndExecutionIntegrationTests(IntegrationTestFixture fixture)
    : EdgeOperationalIntegrationTestBase(fixture)
{
    [IntegrationFact]
    public async Task CustomerCheckout_CashConfirmation_DispatchesAndCompletesWithSimulatedExecution()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        await EnsureCashPaymentMethodAsync();

        var order = await PlaceCustomerOrderAsync(graph);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);

        var paymentSession = await CreateCashPaymentSessionAsync(order);
        Assert.Equal(Domain.Payments.Enums.PaymentTransactionStatus.Pending, paymentSession.Status);
        Assert.Equal(order.Id, paymentSession.OrderId);

        var staff = new CurrentUserContext
        {
            AccountId = runtime.User.AccountId,
            RoleScopes = [new UserRoleScope("Staff", graph.OrganizationId, graph.StoreId, null)]
        };
        await ConfirmCashPaymentAsync(staff, order.Id, paymentSession.PaymentTransactionId);

        Guid commandId;
        await using (var dispatchedContext = _fixture.CreateDbContext())
        {
            var paidOrder = await dispatchedContext.Orders.SingleAsync(candidate => candidate.Id == order.Id);
            Assert.Equal(OrderStatus.ReadyForFulfillment, paidOrder.Status);
            Assert.Equal(Domain.Orders.Enums.PaymentStatus.Paid, paidOrder.PaymentStatus);
            commandId = await dispatchedContext.EdgeCommands
                .Where(candidate => candidate.OrderId == order.Id && candidate.CommandType == EdgeCommandType.ExecuteOrder)
                .Select(candidate => candidate.Id)
                .SingleAsync();
        }

        await PullAndAcknowledgeAsync(graph, commandId, "Accepted");
        await ReportProductionAsync(
            graph,
            commandId,
            Guid.NewGuid(),
            sequenceNumber: 1,
            status: "Completed",
            runtime.ReleaseId,
            runtime.ReleaseChecksum);

        await using var completedContext = _fixture.CreateDbContext();
        var completedOrder = await completedContext.Orders.SingleAsync(candidate => candidate.Id == order.Id);
        Assert.Equal(OrderStatus.Completed, completedOrder.Status);
        Assert.Equal(Domain.Orders.Enums.PaymentStatus.Paid, completedOrder.PaymentStatus);
        Assert.Equal(90m, await completedContext.IngredientDispenserStates
            .Where(candidate => candidate.Id == graph.DispenserStateId)
            .Select(candidate => candidate.EstimatedQuantity)
            .SingleAsync());
    }

    [IntegrationFact]
    public async Task InventoryReadiness_BlocksDispatchForInactiveTopology()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        var user = runtime.User;
        var releaseId = runtime.ReleaseId;

        await AssertInventoryDispatchBlockedAsync(graph, "inactive");
    }

    private async Task<Application.Orders.PlaceOrder.Results.OrderResult> PlaceCustomerOrderAsync(SmokeGraph graph)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var telemetryOptions = Options.Create(new EdgeTelemetryIngestionOptions());
        var orders = new OrderStore(dbContext);
        var availability = new MenuItemOperationalAvailabilityReader(new MenuStore(dbContext));
        var inventory = new MachineProductionInventoryGate(
            new InventoryReadinessEvaluator(new InventoryStore(dbContext)),
            telemetryOptions);
        var handler = new PlaceOrderCommandHandler(
            orders,
            new NoOpRealtimeNotificationPublisher(),
            new PlaceOrderItemAppender(orders, availability, telemetryOptions, inventory),
            Options.Create(new OrderPaymentWindowOptions()));
        var result = await handler.HandleAsync(new PlaceOrderCommand
        {
            IdempotencyKey = $"customer-attended-{Guid.NewGuid():N}",
            Request = new PlaceOrderRequest
            {
                KioskId = graph.KioskId,
                ClientOrderId = $"customer-{Guid.NewGuid():N}",
                Items = [new PlaceOrderItemRequest { MenuItemId = graph.MenuItemId, Quantity = 1 }]
            }
        });

        Assert.True(result.Succeeded, result.Message);
        return result.Data!;
    }

    private async Task<Application.Payments.PaymentSessions.Results.PaymentSessionResult> CreateCashPaymentSessionAsync(
        Application.Orders.PlaceOrder.Results.OrderResult order)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var telemetryOptions = Options.Create(new EdgeTelemetryIngestionOptions());
        var orders = new OrderStore(dbContext);
        var availability = new MenuItemOperationalAvailabilityReader(new MenuStore(dbContext));
        var inventory = new MachineProductionInventoryGate(
            new InventoryReadinessEvaluator(new InventoryStore(dbContext)),
            telemetryOptions);
        var handler = new CreatePaymentSessionCommandHandler(
            new PaymentStore(dbContext),
            new UnusedPaymentGateway(),
            new OrderPaymentSellabilityGuard(orders, availability, inventory, telemetryOptions));
        var result = await handler.HandleAsync(new CreatePaymentSessionCommand
        {
            OrderId = order.Id,
            IdempotencyKey = $"cash-payment-{Guid.NewGuid():N}",
            Request = new CreatePaymentSessionRequest
            {
                PaymentMethodCode = "cash",
                ExpectedAmount = order.TotalAmount,
                ExpectedCurrency = order.Currency
            }
        });

        Assert.True(result.Succeeded, result.Message);
        return result.Data!;
    }

    private async Task ConfirmCashPaymentAsync(
        CurrentUserContext user,
        Guid orderId,
        Guid paymentTransactionId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var paymentStore = new PaymentStore(dbContext);
        var handler = new ConfirmCashPaymentCommandHandler(
            paymentStore,
            new NoOpRealtimeNotificationPublisher(),
            new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dbContext),
                Options.Create(new OrderExecutionDispatchOptions()),
                new NoOpEdgeCommandWakeUpPublisher()),
            NullLogger<ConfirmCashPaymentCommandHandler>.Instance);
        var result = await handler.HandleAsync(new ConfirmCashPaymentCommand
        {
            OrderId = orderId,
            PaymentTransactionId = paymentTransactionId,
            UserContext = user,
            Request = new ConfirmCashPaymentRequest { Note = "Integration-test cash confirmation." }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.False(result.Data!.AlreadyConfirmed);
    }

    private async Task EnsureCashPaymentMethodAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        if (await dbContext.PaymentMethods.AnyAsync(candidate => candidate.Code == "cash"))
        {
            return;
        }

        dbContext.PaymentMethods.Add(new Domain.Payments.Entities.PaymentMethod
        {
            Code = "cash",
            Name = "Cash",
            Provider = "Cash",
            MethodType = "Cash",
            IsOnline = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class UnusedPaymentGateway : IPaymentGateway
    {
        public string ProviderCode => "Test";

        public string CreateProviderOrderCode(Guid paymentTransactionId) => throw new NotSupportedException();

        public Task<Application.Payments.Providers.ProviderPaymentSession> CreatePaymentSessionAsync(
            Domain.Payments.Entities.PaymentTransaction paymentTransaction,
            Order order,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Application.Payments.Providers.ProviderPaymentSession?> GetPaymentSessionAsync(
            string providerOrderCode,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Application.Payments.Providers.ProviderPaymentNotification> ParseAndVerifyNotificationAsync(
            string rawPayload,
            string? signature,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [IntegrationFact]
    public async Task PublishedDeployment_ExecutesPaidOrderAndConsumesInventory()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        var user = runtime.User;
        var releaseId = runtime.ReleaseId;

        var orderId = await CreatePaidOrderAsync(graph, quantity: 2);
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchWakeUpPublisher = new NoOpEdgeCommandWakeUpPublisher { PublishResult = false };
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()),
            dispatchWakeUpPublisher);
        var firstDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = orderId,
            DispatchAttemptNo = 1
        });
        Assert.True(firstDispatch.Succeeded, firstDispatch.Message);
        Assert.False(firstDispatch.Data!.Existing);
        var dispatchWakeUp = Assert.Single(dispatchWakeUpPublisher.Notifications);
        Assert.Equal(firstDispatch.Data.EdgeCommandId, dispatchWakeUp.CommandId);
        Assert.Equal(EdgeCommandType.ExecuteOrder, dispatchWakeUp.CommandType);

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
        Assert.False(dispatchWakeUpPublisher.PublishResult);

        await PullAndAcknowledgeAsync(graph, command.Id, "Accepted");
        await using (var acceptedContext = _fixture.CreateDbContext())
        {
            var acceptedOrder = await acceptedContext.Orders.SingleAsync(x => x.Id == orderId);
            Assert.Equal(OrderStatus.Accepted, acceptedOrder.Status);
            Assert.Single(await acceptedContext.OrderStatusHistories
                .Where(x => x.OrderId == orderId && x.ToStatus == OrderStatus.Accepted)
                .ToListAsync());
        }

        var productionJobId = Guid.NewGuid();
        var stockEvidenceEventId = Guid.NewGuid();
        await ReportProductionAsync(
            graph,
            command.Id,
            productionJobId,
            1,
            "Completed",
            releaseId,
            runtime.ReleaseChecksum,
            [new StockMovementEvidenceInput(stockEvidenceEventId, graph.DispenserStateId, 10, null, null, false)]);
        await using (var jobLevelAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.Preparing,
                (await jobLevelAssertionContext.Orders.SingleAsync(x => x.Id == orderId)).Status);
            Assert.Equal(
                OrderItemStatus.Preparing,
                await jobLevelAssertionContext.OrderItems
                    .Where(item => item.OrderId == orderId)
                    .Select(item => item.Status)
                    .SingleAsync());
        }
        await ReportProductionAsync(
            graph,
            command.Id,
            Guid.NewGuid(),
            1,
            "Completed",
            releaseId,
            runtime.ReleaseChecksum,
            productionUnitNo: 2);
        await ReportProductionAsync(
            graph,
            command.Id,
            null,
            1,
            "Running",
            releaseId,
            runtime.ReleaseChecksum);
        await ReportProductionAsync(
            graph,
            command.Id,
            null,
            2,
            "Completed",
            releaseId,
            runtime.ReleaseChecksum);

        await using (var completedContext = _fixture.CreateDbContext())
        {
            Assert.Equal(OrderStatus.Completed, (await completedContext.Orders.SingleAsync(x => x.Id == orderId)).Status);
            var movement = await completedContext.StockMovements.SingleAsync(x => x.SourceEventId == stockEvidenceEventId);
            Assert.Equal(-10, movement.Quantity);
            Assert.Equal("OrderItem", movement.ReferenceType);
            Assert.Equal(
                await completedContext.OrderItems
                    .Where(item => item.OrderId == orderId)
                    .Select(item => (Guid?)item.Id)
                    .SingleAsync(),
                movement.ReferenceId);
            Assert.Equal(orderId, movement.CorrelationId);
            var expectedMovement = await completedContext.StockMovements.SingleAsync(x =>
                x.ReasonCode == "EXPECTED_PRODUCTION_CONSUMPTION" &&
                x.ReferenceType == "OrderItemProductionUnit");
            Assert.Equal(-10, expectedMovement.Quantity);
            Assert.True(expectedMovement.IsEstimated);
            Assert.Equal(80, (await completedContext.IngredientDispenserStates
                .SingleAsync(x => x.Id == graph.DispenserStateId)).EstimatedQuantity);

            var attempts = await new GetOrderExecutionAttemptsQueryHandler(new OrderStore(completedContext))
                .HandleAsync(new GetOrderExecutionAttemptsQuery
                {
                    OrderId = orderId,
                    UserContext = user
                });
            Assert.True(attempts.Succeeded, attempts.Message);
            var attempt = Assert.Single(attempts.Data!);
            Assert.Equal(command.Id, attempt.SourceCommandId);
            Assert.Equal("Completed", attempt.ExecutionStatus);

            var attemptDetail = await new GetExecutionAttemptQueryHandler(new OrderStore(completedContext))
                .HandleAsync(new GetExecutionAttemptQuery
                {
                    OrderId = orderId,
                    SourceCommandId = command.Id,
                    UserContext = user
                });
            Assert.True(attemptDetail.Succeeded, attemptDetail.Message);
            Assert.Equal(2, attemptDetail.Data!.ProductionExecutions.Count);
            Assert.All(attemptDetail.Data.ProductionExecutions,
                productionExecution => Assert.NotEqual(Guid.Empty, productionExecution.SourceProductionJobId));
            var unitOutcome = Assert.Single(attemptDetail.Data.ProductionUnitOutcomes);
            Assert.Equal(2, unitOutcome.CompletedQuantity);
            Assert.Equal(0, unitOutcome.UnreportedQuantity);
            Assert.NotEmpty(attemptDetail.Data.DeliveryAttempts);
            Assert.False(attemptDetail.Data.Provenance.IsRedispatch);
            Assert.Null(attemptDetail.Data.PreviousAttempt);
        }

    }

    [IntegrationFact]
    public async Task PartialFailure_PreservesEvidenceAndAllowsExactUnitRemake()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        var user = runtime.User;
        var releaseId = runtime.ReleaseId;
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()),
            new NoOpEdgeCommandWakeUpPublisher { PublishResult = false });

        var partialOrderId = await CreatePaidOrderAsync(graph, quantity: 3);
        var partialDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = partialOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(partialDispatch.Succeeded, partialDispatch.Message);
        await PullAndAcknowledgeAsync(graph, partialDispatch.Data!.EdgeCommandId, "Accepted");
        var partialStockEventOne = Guid.NewGuid();
        var partialStockEventTwo = Guid.NewGuid();
        await ReportProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, Guid.NewGuid(), 1, "Completed",
            releaseId, runtime.ReleaseChecksum,
            [new StockMovementEvidenceInput(partialStockEventOne, graph.DispenserStateId, 1, null, null, false)],
            productionUnitNo: 1);
        var overlappingUnit = await IngestProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, Guid.NewGuid(), 1, "Completed",
            releaseId, runtime.ReleaseChecksum, productionUnitNo: 1);
        Assert.False(overlappingUnit.Succeeded);
        Assert.Contains("overlaps units", overlappingUnit.Message);
        await ReportProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, Guid.NewGuid(), 1, "Completed",
            releaseId, runtime.ReleaseChecksum,
            [new StockMovementEvidenceInput(partialStockEventTwo, graph.DispenserStateId, 1, null, null, false)],
            productionUnitNo: 2);
        await ReportProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, Guid.NewGuid(), 1, "Failed",
            releaseId, runtime.ReleaseChecksum, productionUnitNo: 3);
        var contradictorySummary = await IngestProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, null, 1, "Completed",
            releaseId, runtime.ReleaseChecksum);
        Assert.False(contradictorySummary.Succeeded);
        Assert.Contains("contradicts production-unit evidence", contradictorySummary.Message);
        await ReportProductionAsync(
            graph, partialDispatch.Data.EdgeCommandId, null, 1, "Failed",
            releaseId, runtime.ReleaseChecksum);

        await using (var partialAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.FulfillmentIssue,
                (await partialAssertionContext.Orders.SingleAsync(order => order.Id == partialOrderId)).Status);
            Assert.Equal(
                OrderItemStatus.Failed,
                await partialAssertionContext.OrderItems.Where(item => item.OrderId == partialOrderId)
                    .Select(item => item.Status).SingleAsync());
            Assert.Equal(2, await partialAssertionContext.ProductionExecutionRecords.CountAsync(record =>
                record.SourceCommandId == partialDispatch.Data.EdgeCommandId &&
                record.Status == ProductionExecutionStatus.Completed));
            Assert.Equal(2, await partialAssertionContext.StockMovements.CountAsync(movement =>
                movement.SourceEventId == partialStockEventOne || movement.SourceEventId == partialStockEventTwo));
            var incident = await partialAssertionContext.ProductionIncidents.SingleAsync(candidate =>
                candidate.SourceCommandId == partialDispatch.Data.EdgeCommandId &&
                candidate.ProductionUnitNo == 3);
            Assert.Equal(ProductionIncidentTrigger.ExecutionFailed, incident.Trigger);
            Assert.Equal(ProductionInspectionOutcome.NotProduced, incident.InspectionOutcome);
            Assert.Equal(ProductionIncidentStatus.Open, incident.Status);
        }

        var remakeRequestId = Guid.NewGuid();
        Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult remake;
        await using (var remakeContext = _fixture.CreateDbContext())
        {
            var remakeHandler = new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(remakeContext),
                Options.Create(new OrderExecutionDispatchOptions()),
                new NoOpEdgeCommandWakeUpPublisher { PublishResult = false });
            var unsafeRemake = await remakeHandler.HandleRemakeAsync(
                Guid.NewGuid(),
                partialOrderId,
                await remakeContext.OrderItems.Where(item => item.OrderId == partialOrderId)
                    .Select(item => item.Id).SingleAsync(),
                1,
                1,
                user.AccountId,
                "Must reject an already produced unit.");
            Assert.False(unsafeRemake.Succeeded);
            Assert.Equal("Every remake unit must have failed with confirmed no physical output.", unsafeRemake.Message);
            var remakeResult = await remakeHandler.HandleRemakeAsync(
                remakeRequestId,
                partialOrderId,
                await remakeContext.OrderItems.Where(item => item.OrderId == partialOrderId)
                    .Select(item => item.Id).SingleAsync(),
                3,
                1,
                user.AccountId,
                "Retry unit 3 after confirmed no physical output.");
            Assert.True(remakeResult.Succeeded, remakeResult.Message);
            remake = remakeResult.Data!;
            var repeatedRemake = await remakeHandler.HandleRemakeAsync(
                remakeRequestId,
                partialOrderId,
                await remakeContext.OrderItems.Where(item => item.OrderId == partialOrderId)
                    .Select(item => item.Id).SingleAsync(),
                3,
                1,
                user.AccountId,
                "Retry unit 3 after confirmed no physical output.");
            Assert.True(repeatedRemake.Succeeded, repeatedRemake.Message);
            Assert.True(repeatedRemake.Data!.Existing);
            Assert.Equal(remake.EdgeCommandId, repeatedRemake.Data.EdgeCommandId);
        }
        await PullAndAcknowledgeAsync(graph, remake.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph, remake.EdgeCommandId, Guid.NewGuid(), 1, "Completed",
            remake.ConfigurationReleaseId, runtime.ReleaseChecksum, productionUnitNo: 3);
        await ReportProductionAsync(
            graph, remake.EdgeCommandId, null, 1, "Completed",
            remake.ConfigurationReleaseId, runtime.ReleaseChecksum);
        await using (var remakeAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.Completed,
                (await remakeAssertionContext.Orders.SingleAsync(order => order.Id == partialOrderId)).Status);
            Assert.Equal(
                OrderItemStatus.Completed,
                await remakeAssertionContext.OrderItems.Where(item => item.OrderId == partialOrderId)
                    .Select(item => item.Status).SingleAsync());
            Assert.Equal(2, await remakeAssertionContext.StockMovements.CountAsync(movement =>
                movement.SourceEventId == partialStockEventOne || movement.SourceEventId == partialStockEventTwo));
        }

    }

    [IntegrationFact]
    public async Task ExecutionFailuresAndStockEvidence_EnforceOperationalInvariants()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        var user = runtime.User;
        var releaseId = runtime.ReleaseId;
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()),
            new NoOpEdgeCommandWakeUpPublisher { PublishResult = false });

        var rejectedOrderId = await CreatePaidOrderAsync(graph);
        var rejectedDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = rejectedOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(rejectedDispatch.Succeeded, rejectedDispatch.Message);
        await PullAndAcknowledgeAsync(graph, rejectedDispatch.Data!.EdgeCommandId, "Rejected", false);

        // An unresolved customer session intentionally blocks the kiosk. Use isolated
        // runtimes for the following independent failure scenarios.
        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var supportOrderId = await CreatePaidOrderAsync(graph);
        var supportDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = supportOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(supportDispatch.Succeeded, supportDispatch.Message);
        await PullAndAcknowledgeAsync(graph, supportDispatch.Data!.EdgeCommandId, "Rejected", true);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var busyOrderId = await CreatePaidOrderAsync(graph);
        var busyDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = busyOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(busyDispatch.Succeeded, busyDispatch.Message);
        await PullAndAcknowledgeAsync(graph, busyDispatch.Data!.EdgeCommandId, "ExecutorBusy");
        await AcknowledgeAsync(graph, busyDispatch.Data.EdgeCommandId, "ExecutorBusy");

        await using (var busyAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.ReadyForFulfillment,
                (await busyAssertionContext.Orders.SingleAsync(x => x.Id == busyOrderId)).Status);
            var busyCommandBeforeRedelivery = await busyAssertionContext.EdgeCommands
                .Include(x => x.DeliveryAttempts)
                .SingleAsync(x => x.Id == busyDispatch.Data.EdgeCommandId);
            Assert.Equal(EdgeCommandStatus.PendingDelivery, busyCommandBeforeRedelivery.Status);
            Assert.Equal(2, busyCommandBeforeRedelivery.DeliveryAttempts.Count);
        }

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var failedOrderId = await CreatePaidOrderAsync(graph);
        var failedDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = failedOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(failedDispatch.Succeeded, failedDispatch.Message);
        await PullAndAcknowledgeAsync(graph, failedDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            failedDispatch.Data.EdgeCommandId,
            null,
            1,
            "Failed",
            releaseId,
            runtime.ReleaseChecksum);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var interventionOrderId = await CreatePaidOrderAsync(graph);
        var interventionDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = interventionOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(interventionDispatch.Succeeded, interventionDispatch.Message);
        await PullAndAcknowledgeAsync(graph, interventionDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            interventionDispatch.Data.EdgeCommandId,
            null,
            1,
            "RequiresManualIntervention",
            releaseId,
            runtime.ReleaseChecksum,
            errorCode: "ControllerFault");

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var concurrentEvidenceOrderId = await CreatePaidOrderAsync(graph, quantity: 2);
        var concurrentEvidenceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = concurrentEvidenceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(concurrentEvidenceDispatch.Succeeded, concurrentEvidenceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, concurrentEvidenceDispatch.Data!.EdgeCommandId, "Accepted");
        var sharedStockEvidenceId = Guid.NewGuid();
        var sharedEvidence = new StockMovementEvidenceInput(
            sharedStockEvidenceId,
            graph.DispenserStateId,
            5,
            95,
            DateTimeOffset.UtcNow,
            false);
        await Task.WhenAll(
            ReportProductionAsync(
                graph,
                concurrentEvidenceDispatch.Data.EdgeCommandId,
                Guid.NewGuid(),
                1,
                "Completed",
                releaseId,
                runtime.ReleaseChecksum,
                [sharedEvidence],
                productionUnitNo: 1),
            ReportProductionAsync(
                graph,
                concurrentEvidenceDispatch.Data.EdgeCommandId,
                Guid.NewGuid(),
                1,
                "Completed",
                releaseId,
                runtime.ReleaseChecksum,
                [sharedEvidence],
                productionUnitNo: 2));

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var reusedEvidenceOrderId = await CreatePaidOrderAsync(graph);
        var reusedEvidenceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = reusedEvidenceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(reusedEvidenceDispatch.Succeeded, reusedEvidenceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, reusedEvidenceDispatch.Data!.EdgeCommandId, "Accepted");
        var reusedEvidence = await IngestProductionAsync(
            graph,
            reusedEvidenceDispatch.Data.EdgeCommandId,
            Guid.NewGuid(),
            1,
            "Completed",
            releaseId,
            runtime.ReleaseChecksum,
            [sharedEvidence with { QuantityConsumed = 1, BalanceAfter = 94 }]);
        Assert.False(reusedEvidence.Succeeded);
        Assert.Equal(400, reusedEvidence.StatusCode);
        Assert.Equal(
            "Stock movement source event id was reused with different evidence.",
            reusedEvidence.Message);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var inconsistentBalanceOrderId = await CreatePaidOrderAsync(graph);
        var inconsistentBalanceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = inconsistentBalanceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(inconsistentBalanceDispatch.Succeeded, inconsistentBalanceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, inconsistentBalanceDispatch.Data!.EdgeCommandId, "Accepted");
        var inconsistentBalanceEvidenceId = Guid.NewGuid();
        var inconsistentBalance = await IngestProductionAsync(
            graph,
            inconsistentBalanceDispatch.Data.EdgeCommandId,
            Guid.NewGuid(),
            1,
            "Completed",
            releaseId,
            runtime.ReleaseChecksum,
            [new StockMovementEvidenceInput(
                inconsistentBalanceEvidenceId,
                graph.DispenserStateId,
                5,
                79,
                null,
                false)]);
        Assert.False(inconsistentBalance.Succeeded);
        Assert.Equal(400, inconsistentBalance.StatusCode);
        Assert.Equal(
            "Reported stock balance does not match the dispenser estimate after consumption.",
            inconsistentBalance.Message);

        await using (var inconsistentBalanceContext = _fixture.CreateDbContext())
        {
            Assert.Equal(100, (await inconsistentBalanceContext.IngredientDispenserStates
                .SingleAsync(state => state.Id == graph.DispenserStateId)).EstimatedQuantity);
            Assert.False(await inconsistentBalanceContext.StockMovements
                .AnyAsync(movement => movement.SourceEventId == inconsistentBalanceEvidenceId));
            Assert.False(await inconsistentBalanceContext.ProductionExecutionRecords
                .AnyAsync(record => record.SourceCommandId == inconsistentBalanceDispatch.Data.EdgeCommandId));
            Assert.Equal(
                OrderStatus.Accepted,
                (await inconsistentBalanceContext.Orders
                    .SingleAsync(order => order.Id == inconsistentBalanceOrderId)).Status);
            Assert.Equal(
                OrderStatus.Accepted,
                (await inconsistentBalanceContext.Orders
                    .SingleAsync(order => order.Id == reusedEvidenceOrderId)).Status);
            Assert.False(await inconsistentBalanceContext.ProductionExecutionRecords
                .AnyAsync(record => record.SourceCommandId == reusedEvidenceDispatch.Data.EdgeCommandId));
        }

        await using (var refillArrangeContext = _fixture.CreateDbContext())
        {
            var state = await refillArrangeContext.IngredientDispenserStates
                .SingleAsync(item => item.Id == graph.DispenserStateId);
            state.EstimatedQuantity = 83;
            await refillArrangeContext.SaveChangesAsync();
        }

        var refillReasonOne = $"CONCURRENT_REFILL_{Guid.NewGuid():N}";
        var refillReasonTwo = $"CONCURRENT_REFILL_{Guid.NewGuid():N}";
        await Task.WhenAll(
            RefillAsync(graph, user, 5, refillReasonOne),
            RefillAsync(graph, user, 5, refillReasonTwo));
        await using (var refillAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(93, (await refillAssertionContext.IngredientDispenserStates
                .SingleAsync(state => state.Id == graph.DispenserStateId)).EstimatedQuantity);
            Assert.Equal(2, await refillAssertionContext.StockMovements.CountAsync(movement =>
                movement.ReasonCode == refillReasonOne || movement.ReasonCode == refillReasonTwo));
        }

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var releaseMismatchOrderId = await CreatePaidOrderAsync(graph);
        var releaseMismatchDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = releaseMismatchOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(releaseMismatchDispatch.Succeeded, releaseMismatchDispatch.Message);
        await PullAndAcknowledgeAsync(graph, releaseMismatchDispatch.Data!.EdgeCommandId, "Accepted");
        await using (var mismatchContext = _fixture.CreateDbContext())
        {
            var mismatchStore = new ExecutionReportStore(mismatchContext);
            var mismatch = await new IngestExecutionReportCommandHandler(
                mismatchStore,
                new NoOpRealtimeNotificationPublisher(),
                Options.Create(new ExecutionReportIngestionOptions()))
                .HandleAsync(new IngestExecutionReportCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    CommandId = releaseMismatchDispatch.Data.EdgeCommandId,
                    SourceEventId = Guid.NewGuid(),
                    SequenceNumber = 1,
                    EdgeCreatedAt = DateTimeOffset.UtcNow,
                    ReportType = "ProductionExecution",
                    Status = "Running",
                    SourceConfigurationReleaseId = releaseId,
                    ReleaseChecksum = new string('f', 64),
                    PhysicalOutputMayHaveOccurred = false
                });
            Assert.False(mismatch.Succeeded);
            Assert.Equal(400, mismatch.StatusCode);
            Assert.Equal("Production execution report release does not match the dispatched command.", mismatch.Message);
        }

        await using var ackAssertionContext = _fixture.CreateDbContext();
        Assert.Equal(
            OrderStatus.ExecutionRejected,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == rejectedOrderId)).Status);
        Assert.Equal(
            OrderStatus.RefundRequired,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == supportOrderId)).Status);
        Assert.Equal(
            OrderStatus.FulfillmentIssue,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == failedOrderId)).Status);
        Assert.Equal(
            OrderStatus.FulfillmentIssue,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == interventionOrderId)).Status);
        Assert.Single(await ackAssertionContext.StockMovements
            .Where(x => x.SourceEventId == sharedStockEvidenceId)
            .ToListAsync());
        Assert.Empty(await ackAssertionContext.ProductionExecutionRecords
            .Where(x => x.SourceCommandId == releaseMismatchDispatch.Data.EdgeCommandId)
            .ToListAsync());

        var crossOrderAttemptDetail = await new GetExecutionAttemptQueryHandler(new OrderStore(ackAssertionContext))
            .HandleAsync(new GetExecutionAttemptQuery
            {
                OrderId = interventionOrderId,
                SourceCommandId = failedDispatch.Data.EdgeCommandId,
                UserContext = user
            });
        Assert.False(crossOrderAttemptDetail.Succeeded);
        Assert.Equal(404, crossOrderAttemptDetail.StatusCode);

    }

    [IntegrationFact]
    public async Task TimeoutAndRedispatch_EnforceRecoveryInvariants()
    {
        var runtime = await CreateActiveRuntimeAsync();
        var graph = runtime.Graph;
        var user = runtime.User;
        var releaseId = runtime.ReleaseId;
        var expiryUser = user;
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()),
            new NoOpEdgeCommandWakeUpPublisher { PublishResult = false });

        var expiryOrderId = await CreatePaidOrderAsync(graph);
        var expiryBase = DateTimeOffset.UtcNow;
        var expiryDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = expiryOrderId,
            DispatchAttemptNo = 1,
            CommandExpiryAt = expiryBase.AddMinutes(1)
        });
        Assert.True(expiryDispatch.Succeeded, expiryDispatch.Message);
        await ReconcileTimeoutAsync(graph, expiryDispatch.Data!.EdgeCommandId, expiryBase.AddMinutes(2));

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var unreachableGraph = graph;
        var unreachableOrderId = await CreatePaidOrderAsync(graph);
        var unreachableDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = unreachableOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(unreachableDispatch.Succeeded, unreachableDispatch.Message);
        await PullAndAcknowledgeAsync(graph, unreachableDispatch.Data!.EdgeCommandId, "Accepted");
        var unreachableObservedAt = DateTimeOffset.UtcNow.AddMinutes(6);
        var unreachablePublisher = await ReconcileTimeoutAsync(
            graph,
            unreachableDispatch.Data.EdgeCommandId,
            unreachableObservedAt);
        Assert.Single(unreachablePublisher.OrderExecutionObservationEvents);
        Assert.Equal("PendingRecovery", unreachablePublisher.OrderExecutionObservationEvents[0].CustomerStatus);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var staleOrderId = await CreatePaidOrderAsync(graph);
        var staleDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = staleOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(staleDispatch.Succeeded, staleDispatch.Message);
        await PullAndAcknowledgeAsync(graph, staleDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            staleDispatch.Data.EdgeCommandId,
            null,
            1,
            "Running",
            releaseId,
            runtime.ReleaseChecksum);
        await using (var rollbackClockContext = _fixture.CreateDbContext())
        {
            await rollbackClockContext.OrderExecutionRecords
                .Where(record => record.SourceCommandId == staleDispatch.Data.EdgeCommandId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    record => record.LastExecutorReportedAt,
                    DateTimeOffset.UtcNow.AddDays(-1)));
            var timeoutStore = new OrderExecutionTimeoutStore(rollbackClockContext);
            var now = DateTimeOffset.UtcNow;
            var freshCandidates = await timeoutStore.ListCandidateCommandIdsAsync(
                now,
                now.AddMinutes(-5),
                now.AddMinutes(-30),
                100);
            Assert.DoesNotContain(staleDispatch.Data.EdgeCommandId, freshCandidates);
        }
        var staleObservedAt = DateTimeOffset.UtcNow.AddMinutes(31);
        await using (var heartbeatContext = _fixture.CreateDbContext())
        {
            heartbeatContext.KioskHeartbeats.Add(new KioskHeartbeat
            {
                KioskId = graph.KioskId,
                NodeId = graph.SourceExecutorId,
                OriginNodeId = graph.SourceExecutorId,
                Version = 1,
                ReportedAt = staleObservedAt,
                ReceivedAt = staleObservedAt,
                Status = KioskHeartbeatStatus.Online
            });
            await heartbeatContext.SaveChangesAsync();
        }
        await ReconcileTimeoutAsync(graph, staleDispatch.Data.EdgeCommandId, staleObservedAt);

        await using var timeoutAssertionContext = _fixture.CreateDbContext();
        Assert.Equal(OrderStatus.ExecutionRejected,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == expiryOrderId)).Status);
        Assert.Equal(OrderStatus.Accepted,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == unreachableOrderId)).Status);
        Assert.Equal(OrderStatus.Preparing,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == staleOrderId)).Status);
        var unreachableRecord = await timeoutAssertionContext.OrderExecutionRecords
            .SingleAsync(x => x.SourceCommandId == unreachableDispatch.Data.EdgeCommandId);
        Assert.Equal(ExecutionObservationStatus.Unreachable, unreachableRecord.ObservationStatus);
        Assert.Equal(CustomerExecutionStatus.PendingRecovery, unreachableRecord.CustomerExecutionStatus);
        var staleRecord = await timeoutAssertionContext.OrderExecutionRecords
            .SingleAsync(x => x.SourceCommandId == staleDispatch.Data.EdgeCommandId);
        Assert.Equal(ExecutionObservationStatus.Stale, staleRecord.ObservationStatus);
        Assert.Equal(CustomerExecutionStatus.Delayed, staleRecord.CustomerExecutionStatus);

        var supportPublisher = await ReconcileTimeoutAsync(
            unreachableGraph,
            unreachableDispatch.Data.EdgeCommandId,
            staleObservedAt.AddMinutes(10));
        var supportEvent = Assert.Single(supportPublisher.OrderExecutionObservationEvents);
        Assert.Equal("SupportRequired", supportEvent.CustomerStatus);
        Assert.True(supportEvent.RequiresStaffSupport);

        await using (var supportAssertionContext = _fixture.CreateDbContext())
        {
            var supportRecord = await supportAssertionContext.OrderExecutionRecords
                .SingleAsync(x => x.SourceCommandId == unreachableDispatch.Data.EdgeCommandId);
            Assert.Equal(CustomerExecutionStatus.SupportRequired, supportRecord.CustomerExecutionStatus);
            Assert.Equal(OrderStatus.Accepted,
                (await supportAssertionContext.Orders.SingleAsync(x => x.Id == unreachableOrderId)).Status);

            var customerResult = await new GetOrderStatusQueryHandler(new OrderStore(supportAssertionContext))
                .HandleAsync(new GetOrderStatusQuery { OrderId = unreachableOrderId });
            Assert.True(customerResult.Succeeded, customerResult.Message);
            Assert.Equal("SupportRequired", customerResult.Data!.CustomerStatus);
            Assert.True(customerResult.Data.RequiresStaffSupport);
        }

        var redispatch = await RedispatchAsync(expiryOrderId, expiryUser, "Operator confirmed safe retry after expiry.");
        Assert.True(redispatch.Succeeded, redispatch.Message);
        Assert.Equal(2, redispatch.Data!.DispatchAttemptNo);
        Assert.False(redispatch.Data.Existing);
        var repeatedRedispatch = await RedispatchAsync(expiryOrderId, expiryUser, "Repeated client request.");
        Assert.True(repeatedRedispatch.Succeeded, repeatedRedispatch.Message);
        Assert.True(repeatedRedispatch.Data!.Existing);
        Assert.Equal(redispatch.Data.EdgeCommandId, repeatedRedispatch.Data.EdgeCommandId);

        await using (var provenanceContext = _fixture.CreateDbContext())
        {
            var provenanceStore = new OrderStore(provenanceContext);
            var expiredAttemptDetail = await new GetExecutionAttemptQueryHandler(provenanceStore)
                .HandleAsync(new GetExecutionAttemptQuery
                {
                    OrderId = expiryOrderId,
                    SourceCommandId = expiryDispatch.Data.EdgeCommandId,
                    UserContext = expiryUser
                });
            Assert.True(expiredAttemptDetail.Succeeded, expiredAttemptDetail.Message);
            Assert.True(expiredAttemptDetail.Data!.Provenance.TimedOutBeforeAcceptance);
            Assert.Equal(redispatch.Data.EdgeCommandId, expiredAttemptDetail.Data.NextAttempt!.SourceCommandId);

            var redispatchDetail = await new GetExecutionAttemptQueryHandler(provenanceStore)
                .HandleAsync(new GetExecutionAttemptQuery
                {
                    OrderId = expiryOrderId,
                    SourceCommandId = redispatch.Data.EdgeCommandId,
                    UserContext = expiryUser
                });
            Assert.True(redispatchDetail.Succeeded, redispatchDetail.Message);
            Assert.True(redispatchDetail.Data!.Provenance.IsRedispatch);
            Assert.Equal(expiryDispatch.Data.EdgeCommandId, redispatchDetail.Data.Provenance.RetryOfSourceCommandId);
            Assert.Equal(expiryDispatch.Data.EdgeCommandId, redispatchDetail.Data.PreviousAttempt!.SourceCommandId);
            Assert.Contains("Operator confirmed safe retry after expiry.", redispatchDetail.Data.Provenance.RedispatchReason);
        }

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var unsafeOrderId = await CreatePaidOrderAsync(graph);
        var unsafeDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = unsafeOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(unsafeDispatch.Succeeded, unsafeDispatch.Message);
        await PullAndAcknowledgeAsync(graph, unsafeDispatch.Data!.EdgeCommandId, "Rejected", true);

        var unsafeRedispatch = await RedispatchAsync(unsafeOrderId, user, "Unsafe retry must be rejected.");
        Assert.False(unsafeRedispatch.Succeeded);
        Assert.Equal(409, unsafeRedispatch.StatusCode);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var deliveryFailureOrderId = await CreatePaidOrderAsync(graph);
        var deliveryFailureDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = deliveryFailureOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(deliveryFailureDispatch.Succeeded, deliveryFailureDispatch.Message);
        await PullAndAcknowledgeAsync(graph, deliveryFailureDispatch.Data!.EdgeCommandId, "DeliveryFailed");
        var deliveryRedispatch = await RedispatchAsync(
            deliveryFailureOrderId,
            user,
            "Transport delivery failed; retry approved.");
        Assert.True(deliveryRedispatch.Succeeded, deliveryRedispatch.Message);
        Assert.Equal(2, deliveryRedispatch.Data!.DispatchAttemptNo);

        runtime = await CreateActiveRuntimeAsync();
        graph = runtime.Graph;
        user = runtime.User;
        releaseId = runtime.ReleaseId;
        var maxAttemptOrderId = await CreatePaidOrderAsync(graph);
        var maxAttemptDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = maxAttemptOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(maxAttemptDispatch.Succeeded, maxAttemptDispatch.Message);
        await PullAndAcknowledgeAsync(graph, maxAttemptDispatch.Data!.EdgeCommandId, "DeliveryFailed");
        var maxAttemptResult = await RedispatchAsync(
            maxAttemptOrderId,
            user,
            "Attempt limit test.",
            maxDispatchAttempts: 1);
        Assert.False(maxAttemptResult.Succeeded);
        Assert.Equal(409, maxAttemptResult.StatusCode);

        await using var redispatchAssertionContext = _fixture.CreateDbContext();
        var redispatchedCommand = await redispatchAssertionContext.EdgeCommands
            .SingleAsync(x => x.Id == redispatch.Data.EdgeCommandId);
        Assert.Equal(expiryUser.AccountId, redispatchedCommand.CreatedByAccountId);
        Assert.Contains(await redispatchAssertionContext.OrderStatusHistories
            .Where(x => x.OrderId == expiryOrderId && x.ChangedByAccountId == expiryUser.AccountId)
            .Select(x => x.Reason!)
            .ToListAsync(), reason => reason.Contains("Operator confirmed safe retry after expiry."));
    }

}
