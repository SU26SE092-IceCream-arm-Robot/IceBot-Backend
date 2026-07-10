using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Domain.Devices.ExecutionEndpoints;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Reports.Services;

internal sealed record ExecutionReportProcessingContext(
    IngestExecutionReportCommand Command,
    KioskExecutionEndpoint Endpoint,
    Guid SourceExecutorId,
    EdgeCommand EdgeCommand,
    DateTimeOffset ExecutorReportedAt,
    DateTimeOffset CloudReceivedAt,
    ExecutionReportNotifications Notifications);
