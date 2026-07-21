using Application.Identity.Tokens.Claims;
using Application.Tenants.Kiosks.Requests;

namespace Application.Tenants.Kiosks.Commands;

public sealed class SetKioskOperationalStateCommand
{
    public required Guid StoreId { get; init; }
    public required Guid KioskId { get; init; }
    public required SetKioskOperationalStateRequest Request { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
