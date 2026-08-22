using Application.Identity.Tokens.Claims;
using Domain.Orders.Incidents;

namespace Application.Orders.IncidentResolution;

public sealed record ListProductionIncidentsQuery(
    CurrentUserContext UserContext, ProductionIncidentStatus? Status = null,
    Guid? OrganizationId = null, Guid? StoreId = null, Guid? KioskId = null,
    int PageNumber = 1, int PageSize = 20);

public sealed record GetProductionIncidentQuery(CurrentUserContext UserContext, Guid OrderId, Guid IncidentId);

public sealed record OpenProductionIncidentCommand(
    CurrentUserContext UserContext, Guid OrderId, Guid OrderItemId,
    Guid SourceCommandId, Guid SourceProductionJobId, int ProductionUnitNo,
    int ProductionUnitQuantity, string Reason);

public sealed record RecordProductionInspectionCommand(
    CurrentUserContext UserContext, Guid OrderId, Guid IncidentId,
    ProductionInspectionOutcome Outcome, string Reason);

public sealed record SelectProductionIncidentResolutionCommand(
    CurrentUserContext UserContext, Guid OrderId, Guid IncidentId, Guid ResolutionRequestId,
    ProductionIncidentResolution Resolution, string Reason, Guid? PaymentTransactionId = null,
    string? VoucherCode = null, decimal? VoucherValue = null, bool AcknowledgeFullOrderCompensation = false);

public sealed record CompleteProductionIncidentCommand(
    CurrentUserContext UserContext, Guid OrderId, Guid IncidentId, string Notes);

public sealed class ProductionIncidentResult
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid OrderId { get; init; }
    public Guid OrderItemId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public string ProductVariantName { get; init; } = null!;
    public Guid SourceCommandId { get; init; }
    public Guid SourceProductionJobId { get; init; }
    public int ProductionUnitNo { get; init; }
    public int ProductionUnitQuantity { get; init; }
    public string Trigger { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string PhysicalOutputState { get; init; } = null!;
    public string? InspectionOutcome { get; init; }
    public string? Resolution { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RelatedEdgeCommandId { get; init; }
    public Guid? RelatedRefundId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? InspectedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public IReadOnlyCollection<ProductionIncidentHistoryResult> History { get; init; } = [];
}

public sealed record ProductionIncidentHistoryResult(
    Guid Id, string Action, string? FromStatus, string ToStatus, Guid? ActorAccountId,
    string Reason, DateTimeOffset OccurredAt, Guid? RelatedEntityId);

internal static class ProductionIncidentResultMapper
{
    public static ProductionIncidentResult Map(ProductionIncident incident, bool includeHistory = true) => new()
    {
        Id = incident.Id,
        OrganizationId = incident.OrganizationId,
        StoreId = incident.StoreId,
        KioskId = incident.KioskId,
        OrderId = incident.OrderId,
        OrderItemId = incident.OrderItemId,
        OrderNumber = incident.OrderNumberSnapshot,
        ProductName = incident.ProductNameSnapshot,
        ProductVariantName = incident.ProductVariantNameSnapshot,
        SourceCommandId = incident.SourceCommandId,
        SourceProductionJobId = incident.SourceProductionJobId,
        ProductionUnitNo = incident.ProductionUnitNo,
        ProductionUnitQuantity = incident.ProductionUnitQuantity,
        Trigger = incident.Trigger.ToString(),
        Status = incident.Status.ToString(),
        PhysicalOutputState = incident.PhysicalOutputState.ToString(),
        InspectionOutcome = incident.InspectionOutcome?.ToString(),
        Resolution = incident.Resolution?.ToString(),
        ErrorCode = incident.ErrorCodeSnapshot,
        ErrorMessage = incident.ErrorMessageSnapshot,
        RelatedEdgeCommandId = incident.RelatedEdgeCommandId,
        RelatedRefundId = incident.RelatedRefundId,
        CreatedAt = incident.CreatedAt,
        InspectedAt = incident.InspectedAt,
        ResolvedAt = incident.ResolvedAt,
        ResolutionNotes = incident.ResolutionNotes,
        History = includeHistory ? incident.History.OrderBy(x => x.OccurredAt).Select(x =>
            new ProductionIncidentHistoryResult(x.Id, x.Action, x.FromStatus?.ToString(), x.ToStatus.ToString(),
                x.ActorAccountId, x.Reason, x.OccurredAt, x.RelatedEntityId)).ToArray() : []
    };
}

