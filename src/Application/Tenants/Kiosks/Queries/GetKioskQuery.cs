using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Kiosks.Queries;

public sealed class GetKioskQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid KioskId { get; init; }
}
