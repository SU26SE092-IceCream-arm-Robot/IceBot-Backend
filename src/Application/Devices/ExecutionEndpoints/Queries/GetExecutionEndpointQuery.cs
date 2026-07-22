using Application.Identity.Tokens.Claims;

namespace Application.Devices.ExecutionEndpoints.Queries;

public sealed class GetExecutionEndpointQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
}
