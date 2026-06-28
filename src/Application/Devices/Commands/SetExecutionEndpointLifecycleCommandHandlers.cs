using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Domain.Common;

namespace Application.Devices.Commands;

internal static class ExecutionEndpointLifecycleHandler
{
    public static async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(
        IExecutionEndpointStore store, CurrentUserContext userContext, Guid endpointId,
        Action<Domain.Devices.Entities.KioskExecutionEndpoint> transition, string message,
        CancellationToken cancellationToken)
    {
        var loaded = await ExecutionEndpointCommandRules.LoadAccessibleAsync(store, userContext, endpointId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        try
        {
            transition(loaded.Endpoint!);
            loaded.Endpoint!.UpdatedByAccountId = userContext.AccountId;
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<ExecutionEndpointResult>.Success(ExecutionEndpointResultMapper.ToResult(loaded.Endpoint), message);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionEndpointResult>.Fail(ex.Message, 400);
        }
    }
}

public sealed class DisableExecutionEndpointCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public DisableExecutionEndpointCommandHandler(IExecutionEndpointStore store) => _store = store;
    public Task<ApiResult<ExecutionEndpointResult>> HandleAsync(DisableExecutionEndpointCommand command, CancellationToken cancellationToken = default) =>
        ExecutionEndpointLifecycleHandler.HandleAsync(_store, command.UserContext, command.EndpointId, endpoint => endpoint.Disable(), "Execution endpoint disabled successfully.", cancellationToken);
}

public sealed class ReactivateExecutionEndpointCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public ReactivateExecutionEndpointCommandHandler(IExecutionEndpointStore store) => _store = store;
    public Task<ApiResult<ExecutionEndpointResult>> HandleAsync(ReactivateExecutionEndpointCommand command, CancellationToken cancellationToken = default) =>
        ExecutionEndpointLifecycleHandler.HandleAsync(_store, command.UserContext, command.EndpointId, endpoint => endpoint.ReactivateWithCurrentCredential(DateTimeOffset.UtcNow), "Execution endpoint reactivated successfully.", cancellationToken);
}

public sealed class RetireExecutionEndpointCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public RetireExecutionEndpointCommandHandler(IExecutionEndpointStore store) => _store = store;
    public Task<ApiResult<ExecutionEndpointResult>> HandleAsync(RetireExecutionEndpointCommand command, CancellationToken cancellationToken = default) =>
        ExecutionEndpointLifecycleHandler.HandleAsync(_store, command.UserContext, command.EndpointId, endpoint => endpoint.Retire(), "Execution endpoint retired successfully.", cancellationToken);
}
