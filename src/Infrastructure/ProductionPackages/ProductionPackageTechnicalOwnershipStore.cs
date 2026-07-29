using Application.ProductionPackages.Ownership;
using Domain.ProductionPackages;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionPackages;

public sealed class ProductionPackageTechnicalOwnershipStore(IceBotDbContext db)
    : IProductionPackageTechnicalOwnershipStore
{
    public Task<bool> IsPackageManagedAsync(
        ProductionPackageResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var targetKey = resourceId.ToString("D");
        return db.ProductionPackageMaterializations.AsNoTracking().AnyAsync(materialization =>
            materialization.ResourceKind == resourceKind &&
            materialization.TargetKey == targetKey &&
            (materialization.Installation.Status == ProductionPackageInstallationStatus.Installed ||
             materialization.Installation.Status == ProductionPackageInstallationStatus.Superseded) &&
            materialization.Installation.OwnershipMode == ProductionPackageOwnershipMode.PackageManaged,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductionPackageResourceOwner>> ListOwnersAsync(
        ProductionPackageResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var targetKey = resourceId.ToString("D");
        return await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(materialization => materialization.ResourceKind == resourceKind &&
                materialization.TargetKey == targetKey &&
                (materialization.Installation.Status == ProductionPackageInstallationStatus.Installed ||
                 materialization.Installation.Status == ProductionPackageInstallationStatus.Superseded))
            .Select(materialization => new ProductionPackageResourceOwner(
                materialization.InstallationId,
                materialization.Installation.OrganizationId,
                materialization.Installation.StoreId,
                materialization.Installation.KioskId,
                materialization.Installation.OwnershipMode.ToString(),
                materialization.Installation.Status.ToString()))
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
