using Application.Abstractions.Realtime;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Commands;
using Application.Tenants.Kiosks.Requests;
using Application.Tenants.Kiosks.Results;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Tenants;

public sealed class KioskOperationalStateCommandTests
{
    [Fact]
    public async Task Maintenance_IsRejectedWhileExecutionIsRunning()
    {
        var graph = CreateGraph();
        graph.Store.HasRunningExecutionAsync(graph.Kiosk.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await graph.Handler.HandleAsync(CreateCommand(
            graph,
            KioskOperationalState.Maintenance));

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(KioskOperationalState.Operational, graph.Kiosk.OperationalState);
        await graph.Store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmergencyStopRequest_IsAllowedWhileExecutionIsRunningAndPublishesAfterCommit()
    {
        var graph = CreateGraph();
        graph.Store.HasRunningExecutionAsync(graph.Kiosk.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await graph.Handler.HandleAsync(CreateCommand(
            graph,
            KioskOperationalState.EmergencyStopRequested));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(KioskOperationalState.EmergencyStopRequested, graph.Kiosk.OperationalState);
        await graph.Store.Received(1).AddOperationalStateTransitionAsync(
            Arg.Is<KioskOperationalStateTransition>(transition =>
                transition.FromState == KioskOperationalState.Operational &&
                transition.ToState == KioskOperationalState.EmergencyStopRequested),
            Arg.Any<CancellationToken>());
        await graph.Publisher.Received(1).PublishKioskOperationalStateChangedAsync(
            Arg.Any<Application.Abstractions.Realtime.Events.KioskOperationalStateChangedEvent>(),
            Arg.Any<CancellationToken>());
    }

    private static TestGraph CreateGraph()
    {
        var storeId = Guid.NewGuid();
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = storeId,
            Status = KioskStatus.Active
        };
        var store = Substitute.For<IKioskStore>();
        store.GetByStoreAndIdAsync(storeId, kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.ExecuteOperationalStateSerializedAsync(
                kiosk.Id,
                Arg.Any<Func<CancellationToken, Task<ApiResult<KioskResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<ApiResult<KioskResult>>>>(1)(
                call.ArgAt<CancellationToken>(2)));
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        return new TestGraph(
            kiosk,
            storeId,
            store,
            publisher,
            new SetKioskOperationalStateCommandHandler(store, publisher));
    }

    private static SetKioskOperationalStateCommand CreateCommand(
        TestGraph graph,
        KioskOperationalState state) => new()
        {
            StoreId = graph.StoreId,
            KioskId = graph.Kiosk.Id,
            UserContext = new CurrentUserContext
            {
                IsSystemAdmin = true,
                AccountId = Guid.NewGuid()
            },
            Request = new SetKioskOperationalStateRequest
            {
                State = state,
                Reason = "Operational safety"
            }
        };

    private sealed record TestGraph(
        Kiosk Kiosk,
        Guid StoreId,
        IKioskStore Store,
        IRealtimeNotificationPublisher Publisher,
        SetKioskOperationalStateCommandHandler Handler);
}
