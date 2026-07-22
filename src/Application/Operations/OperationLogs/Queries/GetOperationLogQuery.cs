using Application.Identity.Tokens.Claims;

namespace Application.Operations.OperationLogs.Queries;

public sealed class GetOperationLogQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid OperationLogId { get; init; }
}
