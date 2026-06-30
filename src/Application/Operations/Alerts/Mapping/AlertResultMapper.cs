using Application.Operations.Alerts.Results;
using Domain.Operations.Entities;

namespace Application.Operations.Alerts.Mapping;

public static class AlertResultMapper
{
    public static AlertResult ToResult(Alert alert) => new()
    {
        Id = alert.Id,
        OrganizationId = alert.Kiosk.OrganizationId,
        StoreId = alert.Kiosk.StoreId,
        KioskId = alert.KioskId,
        DeviceId = alert.DeviceId,
        AlertCode = alert.AlertCode,
        Severity = alert.Severity.ToString(),
        Title = alert.Title,
        Message = alert.Message,
        Status = alert.Status.ToString(),
        SourceType = alert.SourceType,
        SourceId = alert.SourceId,
        RaisedAt = alert.RaisedAt,
        AcknowledgedByAccountId = alert.AcknowledgedByAccountId,
        AcknowledgedAt = alert.AcknowledgedAt,
        ResolvedAt = alert.ResolvedAt,
        ResolutionNotes = alert.ResolutionNotes,
        CreatedAt = alert.CreatedAt,
        UpdatedAt = alert.UpdatedAt
    };
}
