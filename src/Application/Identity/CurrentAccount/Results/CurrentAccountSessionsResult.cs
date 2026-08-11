namespace Application.Identity.CurrentAccount.Results;

public sealed class CurrentAccountSessionsResult
{
    public Guid? CurrentSessionId { get; init; }

    public IReadOnlyList<CurrentAccountSessionResult> Sessions { get; init; } = [];
}
