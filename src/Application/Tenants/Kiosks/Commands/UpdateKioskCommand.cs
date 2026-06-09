using Application.Identity.Tokens.Claims;
using Application.Tenants.Kiosks.Requests;

namespace Application.Tenants.Kiosks.Commands;

public sealed class UpdateKioskCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid KioskId { get; init; }
    public UpdateKioskRequest Request { get; init; } = null!;
}
