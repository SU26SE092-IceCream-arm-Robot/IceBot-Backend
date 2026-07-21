using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Common;

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

    [Fact]
    public void ProductionExecution_RejectsChangedImmutableJobProvenance()
    {
        var now = DateTimeOffset.UtcNow;
        var orderItemId = Guid.NewGuid();
        var workcellId = Guid.NewGuid();
        var record = ProductionExecutionRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), KioskExecutionProfile.LowCostController,
            Guid.NewGuid(), Guid.NewGuid(), 1, now, now, now,
            ProductionExecutionStatus.Accepted, PhysicalOutputState.No, Guid.NewGuid(),
            orderItemId, 1, 1, workcellId, Guid.NewGuid(), new string('a', 64), 7, new string('b', 64));

        var exception = Assert.Throws<DomainRuleException>(() => record.EnsureSameProvenance(
            orderItemId, 1, 1, workcellId, record.ControllerId,
            new string('c', 64), 7, new string('b', 64)));

        Assert.Equal(
            "Production execution report provenance does not match the first report for this source job.",
            exception.Message);
    }
}
