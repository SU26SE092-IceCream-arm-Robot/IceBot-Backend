using Application.Identity.Tokens.Claims;
using Domain.Inventory.Enums;
using System;

namespace Application.Inventory.Commands;

public sealed class RefillDispenserCommand
{
    public required Guid DispenserStateId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required decimal Quantity { get; init; }
    public IngredientLevelStatus? ReportedLevelAfter { get; init; }
    public string? ReasonCode { get; init; }
}
