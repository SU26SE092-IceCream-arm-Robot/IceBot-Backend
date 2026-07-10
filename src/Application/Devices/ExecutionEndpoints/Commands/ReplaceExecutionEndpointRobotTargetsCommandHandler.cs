using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.ExecutionEndpoints.Mapping;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Shared.Wrappers;
using Domain.Common;

namespace Application.Devices.ExecutionEndpoints.Commands;

public sealed class ReplaceExecutionEndpointRobotTargetsCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public ReplaceExecutionEndpointRobotTargetsCommandHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(
        ReplaceExecutionEndpointRobotTargetsCommand command, CancellationToken cancellationToken = default)
    {
        var loaded = await ExecutionEndpointCommandRules.LoadAccessibleAsync(
            _store, command.UserContext, command.KioskId, command.EndpointId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var endpoint = loaded.Endpoint!;

        var normalizedKeys = command.Request.Targets.Select(target =>
            $"{target.RuntimeTargetCode.Trim().ToUpperInvariant()}|{target.MachineModelCode.Trim().ToUpperInvariant()}|{target.DeviceId}").ToList();
        if (normalizedKeys.Count != normalizedKeys.Distinct().Count())
            return ApiResult<ExecutionEndpointResult>.Fail("Supported robot targets must be unique.", 400);

        var resolved = new List<(string RuntimeTargetCode, string MachineModelCode, Domain.Devices.Catalog.Device? Device)>();
        foreach (var target in command.Request.Targets)
        {
            Domain.Devices.Catalog.Device? device = null;
            if (target.DeviceId.HasValue)
            {
                device = await _store.GetDeviceByKioskIdAsync(endpoint.KioskId, target.DeviceId.Value, cancellationToken);
                if (device is null) return ApiResult<ExecutionEndpointResult>.Fail($"Device {target.DeviceId} not found.", 404);
            }
            resolved.Add((target.RuntimeTargetCode, target.MachineModelCode, device));
        }

        try
        {
            var previousTargets = endpoint.ReplaceSupportedRobotTargets(
                resolved.Select(target => (target.RuntimeTargetCode, target.MachineModelCode, target.Device)));
            _store.RemoveSupportedRobotTargets(previousTargets);
            endpoint.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<ExecutionEndpointResult>.Success(
                ExecutionEndpointResultMapper.ToResult(endpoint), "Supported robot targets replaced successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionEndpointResult>.Fail(ex.Message, 400);
        }
    }
}
