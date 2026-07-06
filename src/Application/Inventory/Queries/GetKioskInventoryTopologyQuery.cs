using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed record GetKioskInventoryTopologyQuery(Guid KioskId, CurrentUserContext UserContext);
