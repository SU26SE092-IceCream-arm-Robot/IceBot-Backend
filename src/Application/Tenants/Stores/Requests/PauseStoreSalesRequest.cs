namespace Application.Tenants.Stores.Requests;

public sealed class PauseStoreSalesRequest
{
    public string Reason { get; init; } = null!;

    public DateTimeOffset? ResumeAt { get; init; }
}
