using Application.Devices.ExecutionEndpoints.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.ExecutionEndpoints.Commands;

public sealed class CreateExecutionEndpointCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public required CreateExecutionEndpointRequest Request { get; init; }
}

public sealed class ReplaceExecutionEndpointRobotTargetsCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
    public required ReplaceExecutionEndpointRobotTargetsRequest Request { get; init; }
}

public sealed class ProvisionExecutionEndpointCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
    public required ProvisionExecutionEndpointRequest Request { get; init; }
}

public sealed class DisableExecutionEndpointCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
}

public sealed class ReactivateExecutionEndpointCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
}

public sealed class RetireExecutionEndpointCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
}
