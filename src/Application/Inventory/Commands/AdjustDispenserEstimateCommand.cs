using Application.Identity.Tokens.Claims;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class AdjustDispenserEstimateCommand
{
    public required Guid KioskId { get; init; }
    public required Guid DispenserStateId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required decimal EstimatedQuantity { get; init; }
    public IngredientLevelStatus? ReportedLevelAfter { get; init; }
    public string? ReasonCode { get; init; }
}
