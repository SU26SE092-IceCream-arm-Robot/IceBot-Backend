using Application.Identity.Tokens.Claims;
using Application.Tenants.Kiosks.Requests;

namespace Application.Tenants.Kiosks.Commands;

public sealed class SetKioskStatusCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid KioskId { get; init; }
    public SetKioskStatusRequest Request { get; init; } = null!;
}
