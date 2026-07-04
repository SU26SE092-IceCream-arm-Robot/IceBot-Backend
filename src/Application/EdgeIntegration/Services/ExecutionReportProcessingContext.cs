using Application.EdgeIntegration.Commands;
using Domain.Devices.ExecutionEndpoints;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Services;

internal sealed record ExecutionReportProcessingContext(
    IngestExecutionReportCommand Command,
    KioskExecutionEndpoint Endpoint,
    Guid SourceExecutorId,
    EdgeCommand EdgeCommand,
    DateTimeOffset ExecutorReportedAt,
    DateTimeOffset CloudReceivedAt,
    ExecutionReportNotifications Notifications);
