using Application.SalesCatalog.ReadModels;
using Domain.Devices.Connectivity;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;

namespace Application.SalesCatalog.Admission.Abstractions;

public interface IOperationalAdmissionReadStore
{
    Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<KioskConnectivityProjection?> GetKioskConnectivityAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveCustomerSessionAsync(
        Guid kioskId,
        DateTimeOffset observedAt,
        Guid? excludingOrderId = null,
        CancellationToken cancellationToken = default);
    Task<MenuItem?> GetMenuItemForKioskAsync(
        Guid menuItemId,
        Guid? organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default);
    Task<ActiveProductionRouteOptionPolicy?> GetActiveProductionRouteOptionPolicyAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken = default);
    Task<bool> IsMenuItemPausedAsync(Guid kioskId, Guid menuItemId, CancellationToken cancellationToken = default);
}
