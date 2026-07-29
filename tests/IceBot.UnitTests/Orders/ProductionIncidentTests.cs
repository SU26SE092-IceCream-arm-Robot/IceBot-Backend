using Domain.Common;
using Domain.Orders.Incidents;
using Domain.ProductionExecution.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class ProductionIncidentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnknownOutput_RequiresInspectionBeforeResolution()
    {
        var incident = Create(PhysicalOutputState.Unknown);

        Assert.Equal(ProductionIncidentStatus.AwaitingInspection, incident.Status);
        Assert.Null(incident.InspectionOutcome);
        Assert.Throws<DomainRuleException>(() => incident.SelectResolution(
            ProductionIncidentResolution.AwaitTechnicalReview, Guid.NewGuid(), Fingerprint(), Guid.NewGuid(), "Review", Now));
    }

    [Fact]
    public void DefectiveOutput_CanAuthorizeExactRemakeAndComplete()
    {
        var incident = Create(PhysicalOutputState.Yes);
        var actorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var edgeCommandId = Guid.NewGuid();

        incident.RecordInspection(ProductionInspectionOutcome.Defective, actorId, "Discarded after inspection", Now);
        incident.SelectResolution(ProductionIncidentResolution.RequestRemake, requestId, Fingerprint(), actorId, "Remake unit", Now);
        incident.RecordResolutionAction(edgeCommandId, null, actorId, "Command queued", Now);
        incident.Resolve(actorId, "Replacement delivered", Now);

        Assert.Equal(ProductionIncidentStatus.Resolved, incident.Status);
        Assert.Equal(edgeCommandId, incident.RelatedEdgeCommandId);
        Assert.Equal(5, incident.History.Count);
    }

    [Fact]
    public void ConfirmedGoodOutput_CannotBeRemade()
    {
        var incident = Create(PhysicalOutputState.Yes);
        var actorId = Guid.NewGuid();
        incident.RecordInspection(ProductionInspectionOutcome.ConfirmedGood, actorId, "Output accepted", Now);

        Assert.Throws<DomainRuleException>(() => incident.SelectResolution(
            ProductionIncidentResolution.RequestRemake, Guid.NewGuid(), Fingerprint(), actorId, "Not eligible", Now));
    }

    [Fact]
    public void RepeatingSameResolutionRequest_IsIdempotent()
    {
        var incident = Create(PhysicalOutputState.No);
        var actorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var fingerprint = Fingerprint();
        incident.SelectResolution(ProductionIncidentResolution.RequestRemake, requestId, fingerprint, actorId, "Retry missing unit", Now);
        var historyCount = incident.History.Count;
        incident.SelectResolution(ProductionIncidentResolution.RequestRemake, requestId, fingerprint, actorId, "Retry missing unit", Now);

        Assert.Equal(historyCount, incident.History.Count);
        Assert.Throws<DomainRuleException>(() => incident.SelectResolution(
            ProductionIncidentResolution.RequestRemake, requestId, new string('b', 64), actorId,
            "Changed payload", Now));
        Assert.Throws<DomainRuleException>(() => incident.SelectResolution(
            ProductionIncidentResolution.RequestRefund, Guid.NewGuid(), Fingerprint(), actorId, "Different resolution", Now));
    }

    [Fact]
    public void SelectedExternalResolution_CannotBeCompletedBeforeActionStarts()
    {
        var incident = Create(PhysicalOutputState.No);
        var actorId = Guid.NewGuid();
        incident.SelectResolution(
            ProductionIncidentResolution.RequestRemake, Guid.NewGuid(), Fingerprint(), actorId, "Remake missing output", Now);

        Assert.Throws<DomainRuleException>(() =>
            incident.Resolve(actorId, "Cannot bypass failed dispatch", Now));
        Assert.Throws<DomainRuleException>(() => incident.RecordInspection(
            ProductionInspectionOutcome.Defective, actorId, "Cannot rewind resolution", Now));
    }

    private static ProductionIncident Create(PhysicalOutputState outputState) =>
        ProductionIncident.OpenFromExecution(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), 1, 1, ProductionIncidentTrigger.ExecutionFailed,
            outputState, "ORD-001", "Ice cream", "Vanilla", "E01", "Execution failed", Now);

    private static string Fingerprint() => new('a', 64);
}
