using Application.Abstractions.Realtime;
using Application.EdgeIntegration.Abstractions;
using Application.Inventory.Abstractions;
using Application.Inventory.Observations;
using Domain.Catalog.Entities;
using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Inventory;

public sealed class InventorySensorObservationIngestionTests
{
    [Fact]
    public async Task Ingest_applies_a_matching_endpoint_observation_and_publishes_projection_change()
    {
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var executorId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var state = CreateState(kioskId, deviceId);
        var endpoint = CreateActiveEndpoint(kioskId, endpointId, executorId);
        var observations = Substitute.For<IInventorySensorObservationStore>();
        var endpoints = Substitute.For<IExecutionEndpointTransportAuthStore>();
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        ConfigureTransaction(observations);
        endpoints.GetEndpointAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);
        observations.GetDispenserStateAsync(state.Id, Arg.Any<CancellationToken>()).Returns(state);
        observations.GetLatestAppliedSequenceAsync(executorId, state.Id, Arg.Any<CancellationToken>()).Returns((long?)null);

        var handler = new IngestInventorySensorObservationsCommandHandler(observations, endpoints, publisher);
        var observedAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        var result = await handler.HandleAsync(new IngestInventorySensorObservationsCommand
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = executorId,
            Observations =
            [
                new InventorySensorObservationInput
                {
                    SourceEventId = Guid.NewGuid(),
                    IngredientDispenserStateId = state.Id,
                    DeviceId = deviceId,
                    ObservationSequence = 7,
                    ObservedLevelStatus = IngredientLevelStatus.Low,
                    ObservedAt = observedAt
                }
            ]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.AppliedCount);
        Assert.Equal(IngredientLevelStatus.Low, state.CurrentLevelStatus);
        Assert.Equal(observedAt, state.LastMeasuredAt);
        await observations.Received(1).AddObservationAsync(
            Arg.Is<InventorySensorObservation>(item =>
                item.Disposition == InventorySensorObservationDisposition.Unbound &&
                item.ObservationSequence == 7 &&
                item.IngredientDispenserStateId == state.Id),
            Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishInventoryChangedAsync(
            Arg.Is<Application.Abstractions.Realtime.Events.InventoryChangedEvent>(item => item.DispenserStateId == state.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ingest_records_out_of_order_evidence_without_rewinding_projection()
    {
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var executorId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var state = CreateState(kioskId, deviceId);
        state.RecordSensorLevel(IngredientLevelStatus.Full, DateTimeOffset.UtcNow);
        var endpoint = CreateActiveEndpoint(kioskId, endpointId, executorId);
        var observations = Substitute.For<IInventorySensorObservationStore>();
        var endpoints = Substitute.For<IExecutionEndpointTransportAuthStore>();
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        ConfigureTransaction(observations);
        endpoints.GetEndpointAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);
        observations.GetDispenserStateAsync(state.Id, Arg.Any<CancellationToken>()).Returns(state);
        observations.GetLatestAppliedSequenceAsync(executorId, state.Id, Arg.Any<CancellationToken>()).Returns(9L);

        var handler = new IngestInventorySensorObservationsCommandHandler(observations, endpoints, publisher);
        var result = await handler.HandleAsync(new IngestInventorySensorObservationsCommand
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = executorId,
            Observations =
            [
                new InventorySensorObservationInput
                {
                    SourceEventId = Guid.NewGuid(),
                    IngredientDispenserStateId = state.Id,
                    DeviceId = deviceId,
                    ObservationSequence = 8,
                    ObservedLevelStatus = IngredientLevelStatus.Low,
                    ObservedAt = DateTimeOffset.UtcNow.AddSeconds(-1)
                }
            ]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.OutOfOrderCount);
        Assert.Equal(IngredientLevelStatus.Full, state.CurrentLevelStatus);
        await observations.Received(1).AddObservationAsync(
            Arg.Is<InventorySensorObservation>(item => item.Disposition == InventorySensorObservationDisposition.OutOfOrder),
            Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishInventoryChangedAsync(
            Arg.Any<Application.Abstractions.Realtime.Events.InventoryChangedEvent>(), Arg.Any<CancellationToken>());
    }

    private static void ConfigureTransaction(IInventorySensorObservationStore store)
    {
        store.ExecuteObservationIngestionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<InventorySensorObservationIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<InventorySensorObservationIngestResult>>>>(3)(CancellationToken.None));
    }

    private static IngredientDispenserState CreateState(Guid kioskId, Guid deviceId) => new()
    {
        Id = Guid.NewGuid(),
        KioskId = kioskId,
        DeviceId = deviceId,
        IngredientId = Guid.NewGuid(),
        ContainerCode = "ICE-1",
        Kiosk = new Kiosk { Id = kioskId, OrganizationId = Guid.NewGuid(), StoreId = Guid.NewGuid(), Code = "K-1", Name = "Kiosk" },
        Ingredient = new Ingredient { Id = Guid.NewGuid(), Code = "ICE", Name = "Ice cream" }
    };

    private static KioskExecutionEndpoint CreateActiveEndpoint(Guid kioskId, Guid endpointId, Guid executorId)
    {
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId, "edge-1", KioskExecutionProfile.FullEdge, ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.Id = endpointId;
        endpoint.ProvisionCredential("test", DateTimeOffset.UtcNow);
        endpoint.Activate(executorId, DateTimeOffset.UtcNow);
        return endpoint;
    }
}
