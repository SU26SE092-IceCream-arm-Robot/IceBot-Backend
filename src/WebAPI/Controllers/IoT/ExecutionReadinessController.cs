using Domain.Devices.ExecutionEndpoints;
using System.ComponentModel.DataAnnotations;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Devices.Catalog;
using Domain.ProductionExecution.Enums;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;
using Application.Devices.Connectivity.Contracts;

namespace WebAPI.Controllers.IoT;

[ApiController, ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/readiness")]
public sealed class ExecutionReadinessController : ControllerBase
{
    private readonly IngestExecutionReadinessCommandHandler _handler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;
    public ExecutionReadinessController(IngestExecutionReadinessCommandHandler handler, ExecutionEndpointRequestAuthenticator authenticator)
    { _handler = handler; _authenticator = authenticator; }

    [HttpPost]
    public async Task<IActionResult> Ingest(Guid endpointId,
        [FromBody] ExecutionReadinessRequest request, CancellationToken ct)
    {
        var auth = await _authenticator.AuthenticateAsync(HttpContext, endpointId, ct);
        if (!auth.Succeeded) return Unauthorized(ApiResult<object>.Fail(auth.Message, 401));
        var result = await _handler.HandleAsync(new IngestExecutionReadinessCommand
        {
            KioskId = auth.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = request.SourceExecutorId,
            StateRevision = request.StateRevision,
            ExecutorReportedAt = request.ExecutorReportedAt,
            Readiness = request.Readiness,
            Activity = request.Activity,
            Safety = request.Safety,
            CurrentCommandId = request.CurrentCommandId,
            PhysicalOutputState = request.PhysicalOutputState,
            FaultCode = request.FaultCode,
            LocalPersistenceHealth = new LocalPersistenceHealthInput(
                request.LocalPersistenceHealth.StorageWritable,
                request.LocalPersistenceHealth.FreeSpaceBytes,
                request.LocalPersistenceHealth.MinimumRequiredFreeSpaceBytes,
                request.LocalPersistenceHealth.LocalDatabaseHealth,
                request.LocalPersistenceHealth.PendingEventCount,
                request.LocalPersistenceHealth.MaximumPendingEventCount),
            Capabilities = request.Capabilities.Select(x => new ExecutionCapabilityInput(x.CapabilityCode, x.WorkcellCode, x.IsAvailable, x.UnavailableReason)).ToArray()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }
}
public sealed class ExecutionReadinessRequest
{
    [Required] public Guid SourceExecutorId { get; init; }
    [Range(1, long.MaxValue)] public long StateRevision { get; init; }
    [Required] public DateTimeOffset ExecutorReportedAt { get; init; }
    public ExecutionReadinessState Readiness { get; init; }
    public ExecutionActivityState Activity { get; init; }
    public ExecutionSafetyState Safety { get; init; }
    public Guid? CurrentCommandId { get; init; }
    public PhysicalOutputState PhysicalOutputState { get; init; } = PhysicalOutputState.Unknown;
    [StringLength(100)] public string? FaultCode { get; init; }
    [Required] public LocalPersistenceHealthRequest LocalPersistenceHealth { get; init; } = null!;
    [MaxLength(200)] public IReadOnlyList<ExecutionCapabilityRequest> Capabilities { get; init; } = [];
}
public sealed class LocalPersistenceHealthRequest
{
    public bool StorageWritable { get; init; }
    [Range(0, long.MaxValue)] public long FreeSpaceBytes { get; init; }
    [Range(1, long.MaxValue)] public long MinimumRequiredFreeSpaceBytes { get; init; }
    public LocalDatabaseHealth LocalDatabaseHealth { get; init; }
    [Range(0, int.MaxValue)] public int PendingEventCount { get; init; }
    [Range(1, int.MaxValue)] public int MaximumPendingEventCount { get; init; }
}
public sealed class ExecutionCapabilityRequest
{
    [Required, StringLength(100)] public string CapabilityCode { get; init; } = "";
    [StringLength(100)] public string? WorkcellCode { get; init; }
    public bool IsAvailable { get; init; }
    [StringLength(500)] public string? UnavailableReason { get; init; }
}
