using Domain.Common;
using Domain.ProductionExecution.Enums;

namespace Domain.Orders.Incidents;

public sealed class ProductionIncident : BusinessEntity
{
    private readonly List<ProductionIncidentHistory> _history = [];

    public Guid? OrganizationId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid KioskId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid SourceCommandId { get; private set; }
    public Guid SourceProductionJobId { get; private set; }
    public string OrderNumberSnapshot { get; private set; } = null!;
    public string ProductNameSnapshot { get; private set; } = null!;
    public string ProductVariantNameSnapshot { get; private set; } = null!;
    public int ProductionUnitNo { get; private set; }
    public int ProductionUnitQuantity { get; private set; }
    public ProductionIncidentTrigger Trigger { get; private set; }
    public ProductionIncidentStatus Status { get; private set; }
    public PhysicalOutputState PhysicalOutputState { get; private set; }
    public ProductionInspectionOutcome? InspectionOutcome { get; private set; }
    public ProductionIncidentResolution? Resolution { get; private set; }
    public string? ErrorCodeSnapshot { get; private set; }
    public string? ErrorMessageSnapshot { get; private set; }
    public Guid? OpenedByAccountId { get; private set; }
    public Guid? InspectedByAccountId { get; private set; }
    public DateTimeOffset? InspectedAt { get; private set; }
    public Guid? ResolutionRequestId { get; private set; }
    public string? ResolutionRequestFingerprint { get; private set; }
    public Guid? RelatedEdgeCommandId { get; private set; }
    public Guid? RelatedRefundId { get; private set; }
    public Guid? ResolvedByAccountId { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public IReadOnlyCollection<ProductionIncidentHistory> History => _history;

    private ProductionIncident() { }

    public static ProductionIncident OpenFromExecution(
        Guid? organizationId, Guid? storeId, Guid kioskId, Guid orderId, Guid orderItemId,
        Guid sourceCommandId, Guid sourceProductionJobId, int unitNo, int quantity,
        ProductionIncidentTrigger trigger, PhysicalOutputState physicalOutputState,
        string orderNumber, string productName, string productVariantName,
        string? errorCode, string? errorMessage, DateTimeOffset openedAt,
        Guid? openedByAccountId = null)
    {
        ValidateIdentity(kioskId, orderId, orderItemId, sourceCommandId, sourceProductionJobId, unitNo, quantity);
        if (openedByAccountId == Guid.Empty)
            throw new DomainRuleException("Production incident opener is invalid.");
        var inferredInspection = physicalOutputState == PhysicalOutputState.No
            ? ProductionInspectionOutcome.NotProduced
            : (ProductionInspectionOutcome?)null;
        var incident = new ProductionIncident
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            OrderId = orderId,
            OrderItemId = orderItemId,
            SourceCommandId = sourceCommandId,
            SourceProductionJobId = sourceProductionJobId,
            ProductionUnitNo = unitNo,
            ProductionUnitQuantity = quantity,
            Trigger = trigger,
            PhysicalOutputState = physicalOutputState,
            OrderNumberSnapshot = RequireText(orderNumber, "Order number"),
            ProductNameSnapshot = RequireText(productName, "Product name"),
            ProductVariantNameSnapshot = RequireText(productVariantName, "Product variant name"),
            Status = inferredInspection.HasValue ? ProductionIncidentStatus.Open : ProductionIncidentStatus.AwaitingInspection,
            InspectionOutcome = inferredInspection,
            ErrorCodeSnapshot = Normalize(errorCode, 100),
            ErrorMessageSnapshot = Normalize(errorMessage, 500),
            OpenedByAccountId = openedByAccountId,
            CreatedAt = openedAt
        };
        incident.AddHistory("OpenedFromExecution", null, incident.Status, openedByAccountId,
            "Execution evidence opened incident.", openedAt);
        return incident;
    }

    public void RecordInspection(ProductionInspectionOutcome outcome, Guid actorId, string reason, DateTimeOffset at)
    {
        EnsureActive();
        if (Status is not (ProductionIncidentStatus.Open or ProductionIncidentStatus.AwaitingInspection))
            throw new DomainRuleException("Inspection cannot change after a resolution has been selected.");
        if (!Enum.IsDefined(outcome))
            throw new DomainRuleException("Inspection outcome is invalid.");
        ValidateActorText(actorId, reason, "Inspection reason");
        var from = Status;
        InspectionOutcome = outcome;
        InspectedByAccountId = actorId;
        InspectedAt = at;
        Status = ProductionIncidentStatus.Open;
        UpdatedAt = at;
        AddHistory("InspectionRecorded", from, Status, actorId, reason, at);
    }

    public void SelectResolution(ProductionIncidentResolution resolution, Guid requestId,
        string requestFingerprint, Guid actorId, string reason, DateTimeOffset at)
    {
        if (!Enum.IsDefined(resolution))
            throw new DomainRuleException("Production incident resolution is invalid.");
        if (requestId == Guid.Empty)
            throw new DomainRuleException("Resolution request identity is required.");
        if (string.IsNullOrWhiteSpace(requestFingerprint) || requestFingerprint.Length != 64)
            throw new DomainRuleException("Resolution request fingerprint is invalid.");
        ValidateActorText(actorId, reason, "Resolution reason");
        if (ResolutionRequestId.HasValue)
        {
            if (ResolutionRequestId == requestId && Resolution == resolution &&
                string.Equals(ResolutionRequestFingerprint, requestFingerprint, StringComparison.Ordinal)) return;
            throw new DomainRuleException("This incident already has a different resolution request.");
        }
        EnsureActive();
        if (!InspectionOutcome.HasValue)
            throw new DomainRuleException("Production output must be inspected before selecting a resolution.");
        ValidateResolution(resolution, InspectionOutcome.Value);
        var from = Status;
        Resolution = resolution;
        ResolutionRequestId = requestId;
        ResolutionRequestFingerprint = requestFingerprint;
        Status = ProductionIncidentStatus.ResolutionSelected;
        UpdatedAt = at;
        AddHistory("ResolutionSelected", from, Status, actorId, reason, at);
    }

    public void RecordResolutionAction(Guid? edgeCommandId, Guid? refundId, Guid actorId, string reason, DateTimeOffset at)
    {
        if (Status != ProductionIncidentStatus.ResolutionSelected)
            throw new DomainRuleException("Only a selected resolution can start execution.");
        ValidateActorText(actorId, reason, "Resolution action reason");
        if (Resolution == ProductionIncidentResolution.RequestRemake && (!edgeCommandId.HasValue || refundId.HasValue))
            throw new DomainRuleException("Remake resolution requires only its Edge command identity.");
        if (Resolution is ProductionIncidentResolution.RequestRefund or ProductionIncidentResolution.IssueVoucher &&
            (!refundId.HasValue || edgeCommandId.HasValue))
            throw new DomainRuleException("Compensation resolution requires only its refund identity.");
        if (Resolution is not (ProductionIncidentResolution.RequestRemake or
                ProductionIncidentResolution.RequestRefund or ProductionIncidentResolution.IssueVoucher) &&
            (edgeCommandId.HasValue || refundId.HasValue))
            throw new DomainRuleException("This resolution must not link a remake or refund action.");
        var from = Status;
        RelatedEdgeCommandId = edgeCommandId;
        RelatedRefundId = refundId;
        Status = ProductionIncidentStatus.ResolutionInProgress;
        UpdatedAt = at;
        AddHistory("ResolutionStarted", from, Status, actorId, reason, at, edgeCommandId ?? refundId);
    }

    public void Resolve(Guid actorId, string notes, DateTimeOffset at)
    {
        if (Status != ProductionIncidentStatus.ResolutionInProgress)
            throw new DomainRuleException("Only a started resolution can be completed.");
        ValidateActorText(actorId, notes, "Resolution notes");
        var from = Status;
        Status = ProductionIncidentStatus.Resolved;
        ResolvedByAccountId = actorId;
        ResolvedAt = at;
        ResolutionNotes = notes.Trim();
        UpdatedAt = at;
        AddHistory("Resolved", from, Status, actorId, notes, at, RelatedEdgeCommandId ?? RelatedRefundId);
    }

    private static void ValidateResolution(ProductionIncidentResolution resolution, ProductionInspectionOutcome outcome)
    {
        if (resolution == ProductionIncidentResolution.DeliverExistingOutput && outcome != ProductionInspectionOutcome.ConfirmedGood)
            throw new DomainRuleException("Only confirmed-good output can be delivered.");
        if (resolution == ProductionIncidentResolution.RequestRemake &&
            outcome is not (ProductionInspectionOutcome.NotProduced or ProductionInspectionOutcome.Defective))
            throw new DomainRuleException("Remake requires confirmed missing or defective output.");
    }

    private void EnsureActive()
    {
        if (Status is ProductionIncidentStatus.Resolved or ProductionIncidentStatus.Cancelled)
            throw new DomainRuleException("A closed production incident cannot be changed.");
    }

    private void AddHistory(string action, ProductionIncidentStatus? from, ProductionIncidentStatus to,
        Guid? actorId, string reason, DateTimeOffset at, Guid? relatedEntityId = null) =>
        _history.Add(ProductionIncidentHistory.Create(Id, action, from, to, actorId, reason, at, relatedEntityId));

    private static void ValidateIdentity(Guid kioskId, Guid orderId, Guid orderItemId, Guid commandId,
        Guid jobId, int unitNo, int quantity)
    {
        if (kioskId == Guid.Empty || orderId == Guid.Empty || orderItemId == Guid.Empty || commandId == Guid.Empty ||
            jobId == Guid.Empty || unitNo <= 0 || quantity <= 0)
            throw new DomainRuleException("Production incident provenance and positive unit range are required.");
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static void ValidateActorText(Guid actorId, string value, string field)
    {
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException($"{field} and actor are required.");
        if (value.Trim().Length > 1000)
            throw new DomainRuleException($"{field} cannot exceed 1000 characters.");
    }
    private static string RequireText(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new DomainRuleException($"{field} snapshot is required.")
        : value.Trim();
}
