using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.ProductionExecution.Enums;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace IceBot.UnitTests.Devices;
public sealed class ExecutionReadinessProjectionTests
{
    [Fact]
    public void Projection_AcceptsOnlyNewerRevision()
    {
        var now = DateTimeOffset.UtcNow;
        var projection = ExecutionEndpointReadinessProjection.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4,
            ExecutionReadinessState.Ready, ExecutionActivityState.Idle, ExecutionSafetyState.Safe,
            null, PhysicalOutputState.No, null, now, now);

        Assert.Throws<DomainRuleException>(() => projection.Apply(
            4, ExecutionReadinessState.NotReady, ExecutionActivityState.Busy,
            ExecutionSafetyState.Interlocked, null, PhysicalOutputState.Unknown, "FAULT", now, now));

        projection.Apply(5, ExecutionReadinessState.Degraded, ExecutionActivityState.Idle,
            ExecutionSafetyState.Safe, null, PhysicalOutputState.No, "CAPABILITY_LOSS", now, now);
        Assert.Equal(5, projection.StateRevision);
        Assert.Equal(ExecutionReadinessState.Degraded, projection.Readiness);
    }
}
