using Application.Orders.Support;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using IceBot.UnitTests.TestSupport;

namespace IceBot.UnitTests.Orders;

public sealed class OrderExecutionCustomerProjectionTests
{
    [Fact]
    public void ProjectFromOrder_FulfillmentIssueRequiresStaffSupport()
    {
        var order = new Order();
        TestData.SetProperty(order, nameof(Order.Status), OrderStatus.FulfillmentIssue);

        var projection = OrderStatusProjector.ProjectFromOrder(order);

        Assert.Equal("SupportRequired", projection.CustomerStatus);
        Assert.True(projection.RequiresStaffSupport);
    }

    [Theory]
    [InlineData(ExecutionObservationStatus.Stale, CustomerExecutionStatus.Delayed, "Delayed", false)]
    [InlineData(ExecutionObservationStatus.Unreachable, CustomerExecutionStatus.PendingRecovery, "PendingRecovery", false)]
    [InlineData(ExecutionObservationStatus.Unreachable, CustomerExecutionStatus.SupportRequired, "SupportRequired", true)]
    public void ProjectFromOrderAndExecution_UsesCloudObservationWithoutChangingOrderStatus(
        ExecutionObservationStatus observationStatus,
        CustomerExecutionStatus executionStatus,
        string expectedCustomerStatus,
        bool expectedSupport)
    {
        var order = new Order();
        TestData.SetProperty(order, nameof(Order.Status), OrderStatus.Preparing);
        var record = OrderExecutionRecord.CreateProvisionalAccepted(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            KioskExecutionProfile.FullEdge,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 64),
            DateTimeOffset.UtcNow.AddMinutes(-20));
        record.MarkCloudObservation(observationStatus, executionStatus, DateTimeOffset.UtcNow);

        var projection = OrderStatusProjector.ProjectFromOrderAndExecution(order, record);

        Assert.Equal(expectedCustomerStatus, projection.CustomerStatus);
        Assert.Equal(expectedSupport, projection.RequiresStaffSupport);
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }
}
