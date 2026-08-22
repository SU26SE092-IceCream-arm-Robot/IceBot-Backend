using Application.Devices.Telemetry;
using Application.SalesCatalog.Admission.Abstractions;
using Application.Tenants.Kiosks.Rules;
using Application.Tenants.Stores;
using Domain.Common.Enums;
using Domain.Devices.Connectivity;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Options;

namespace Application.SalesCatalog.Admission.Services;

public sealed class KioskSalesAdmissionEvaluator(
    IOperationalAdmissionReadStore readStore,
    IOptions<KioskSalesAdmissionOptions> salesAdmissionOptions,
    IOptions<EdgeTelemetryIngestionOptions> telemetryOptions)
{
    private readonly KioskSalesAdmissionOptions _salesAdmission = salesAdmissionOptions.Value;
    private readonly EdgeTelemetryIngestionOptions _telemetry = telemetryOptions.Value;

    public async Task<KioskSalesAdmissionDecision?> EvaluateAsync(
        Guid kioskId,
        KioskSalesAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await readStore.GetKioskAsync(kioskId, cancellationToken);
        return kiosk is null
            ? null
            : await EvaluateAsync(kiosk, request, cancellationToken);
    }

    public async Task<KioskSalesAdmissionDecision> EvaluateAsync(
        Kiosk kiosk,
        KioskSalesAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockers = new List<SalesAdmissionBlocker>();
        var now = request.EvaluatedAt;
        KioskConnectivityProjection? connectivity = null;

        AddLifecycleBlockers(kiosk, blockers);
        var canExposeCatalog = blockers.Count == 0;

        if (canExposeCatalog)
        {
            AddStoreAdmissionBlocker(kiosk.Store, now, blockers);

            if (_salesAdmission.RequireConnectivity)
            {
                connectivity = await readStore.GetKioskConnectivityAsync(kiosk.Id, cancellationToken);
                if (connectivity?.Status is not (KioskConnectivityStatus.Online or KioskConnectivityStatus.Degraded))
                {
                    blockers.Add(new SalesAdmissionBlocker(
                        SalesAdmissionBlockerCode.KioskConnectivityUnavailable,
                        SalesAdmissionBlockerScope.Kiosk,
                        kiosk.Id,
                        kiosk.Code));
                }
            }

            if (request.CheckCustomerSession && await readStore.HasActiveCustomerSessionAsync(
                    kiosk.Id, now, request.ExcludingOrderId, cancellationToken))
            {
                blockers.Add(new SalesAdmissionBlocker(
                    SalesAdmissionBlockerCode.CustomerSessionOccupied,
                    SalesAdmissionBlockerScope.Kiosk,
                    kiosk.Id,
                    kiosk.Code));
            }
        }

        return new KioskSalesAdmissionDecision(
            canExposeCatalog,
            CanPlaceOrder: blockers.Count == 0,
            CanOpenPayment: blockers.All(blocker => blocker.Code is
                SalesAdmissionBlockerCode.StoreSalesPaused or SalesAdmissionBlockerCode.StoreClosed),
            blockers,
            [],
            now,
            ResolveEvidenceValidUntil(kiosk.Store, connectivity, now));
    }

    private void AddLifecycleBlockers(Kiosk kiosk, ICollection<SalesAdmissionBlocker> blockers)
    {
        if (kiosk.Organization is null || kiosk.Organization.Status != EntityStatus.Active)
        {
            blockers.Add(new SalesAdmissionBlocker(
                SalesAdmissionBlockerCode.OrganizationInactive,
                SalesAdmissionBlockerScope.Organization,
                kiosk.OrganizationId,
                kiosk.Organization?.Code));
        }

        if (kiosk.Store is null || kiosk.Store.Status != EntityStatus.Active)
        {
            blockers.Add(new SalesAdmissionBlocker(
                SalesAdmissionBlockerCode.StoreInactive,
                SalesAdmissionBlockerScope.Store,
                kiosk.StoreId,
                kiosk.Store?.Code));
        }

        if (kiosk.Status != KioskStatus.Active)
        {
            blockers.Add(new SalesAdmissionBlocker(
                SalesAdmissionBlockerCode.KioskInactive,
                SalesAdmissionBlockerScope.Kiosk,
                kiosk.Id,
                kiosk.Code));
        }

        if (kiosk.OperationalState != KioskOperationalState.Operational)
        {
            blockers.Add(new SalesAdmissionBlocker(
                SalesAdmissionBlockerCode.KioskOperationalHold,
                SalesAdmissionBlockerScope.Kiosk,
                kiosk.Id,
                kiosk.Code));
        }
    }

    private static void AddStoreAdmissionBlocker(
        Store store,
        DateTimeOffset now,
        ICollection<SalesAdmissionBlocker> blockers)
    {
        var error = StoreSalesAvailabilityRules.ValidateSalesAdmission(store, now);
        if (error is null)
        {
            return;
        }

        blockers.Add(new SalesAdmissionBlocker(
            store.IsSalesPausedAt(now)
                ? SalesAdmissionBlockerCode.StoreSalesPaused
                : SalesAdmissionBlockerCode.StoreClosed,
            SalesAdmissionBlockerScope.Store,
            store.Id,
            store.Code));
    }

    private DateTimeOffset? ResolveEvidenceValidUntil(
        Store store,
        KioskConnectivityProjection? connectivity,
        DateTimeOffset now)
    {
        var expiries = new List<DateTimeOffset>();
        if (_salesAdmission.RequireConnectivity && connectivity?.LastObservedAt is { } lastObservedAt)
        {
            expiries.Add(lastObservedAt.AddSeconds(_telemetry.HeartbeatTimeoutSeconds));
        }

        if (store.IsSalesPausedAt(now) && store.SalesPausedUntil is { } pauseUntil)
        {
            expiries.Add(pauseUntil);
        }

        return expiries.Count == 0 ? null : expiries.Min();
    }
}
