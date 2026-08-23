using Application.Inventory.Abstractions;
using Application.SalesCatalog.Admission;
using Domain.Tenants.Entities;

namespace Application.SalesCatalog.Admission.Abstractions;

public interface IMenuItemOperationalAdmissionEvaluator
{
    Task<MenuItemOperationalDecision> EvaluateAsync(
        Kiosk kiosk,
        Guid menuItemId,
        int quantity,
        IReadOnlyCollection<InventoryIngredientRequirementInput>? selectedOptionIngredients,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
