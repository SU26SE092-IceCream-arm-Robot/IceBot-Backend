using Application.Identity.Tokens.Claims;
using Application.Tenants.Kiosks.Requests;

namespace Application.Tenants.Kiosks.Commands;

public sealed class CreateKioskCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid StoreId { get; init; }
    public CreateKioskRequest Request { get; init; } = null!;
}
