using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ProductionExecutionProjectionTests
{
    [Fact]
    public void OrderExecution_AcceptedCanApplyCompletedReplayWithoutRunningObservation()
    {
        var now = DateTimeOffset.UtcNow;
        var record = OrderExecutionRecord.CreateProvisionalAccepted(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), KioskExecutionProfile.LowCostController,
            Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), now);

        var applied = record.ApplyObservation(
            Guid.NewGuid(), 1, now.AddSeconds(1), now.AddSeconds(1), now.AddSeconds(2),
            ProductionExecutionStatus.Completed, ExecutionObservationStatus.Fresh,
            CustomerExecutionStatus.Completed);

        Assert.True(applied);
        Assert.Equal(ProductionExecutionStatus.Completed, record.Status);
    }

    [Fact]
    public void ProductionExecution_AcceptedCanApplyCompletedReplayWithoutRunningObservation()
    {
        var now = DateTimeOffset.UtcNow;
        var record = ProductionExecutionRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), KioskExecutionProfile.LowCostController,
            Guid.NewGuid(), Guid.NewGuid(), 1, now, now, now,
            ProductionExecutionStatus.Accepted, PhysicalOutputState.Unknown, Guid.NewGuid(),
            Guid.NewGuid(), 1, 1);

        var applied = record.ApplyObservation(
            Guid.NewGuid(), 2, now.AddSeconds(1), now.AddSeconds(1), now.AddSeconds(2),
            ProductionExecutionStatus.Completed, PhysicalOutputState.Yes);

        Assert.True(applied);
        Assert.Equal(ProductionExecutionStatus.Completed, record.Status);
    }
}
