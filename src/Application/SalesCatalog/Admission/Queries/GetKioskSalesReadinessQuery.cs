using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks;
using Application.SalesCatalog.Admission.Services;
using Application.Identity.Tokens.Claims;

namespace Application.SalesCatalog.Admission.Queries;

public sealed record GetKioskSalesReadinessQuery(Guid KioskId, CurrentUserContext UserContext);

public sealed class GetKioskSalesReadinessQueryHandler(
    IKioskStore kiosks,
    KioskSalesAdmissionEvaluator evaluator)
{
    public async Task<ApiResult<KioskSalesReadinessResult>> HandleAsync(
        GetKioskSalesReadinessQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await kiosks.GetByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskSalesReadinessResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.KiosksView, query.UserContext, kiosk))
        {
            return ApiResult<KioskSalesReadinessResult>.Fail("Access denied.", 403);
        }

        var decision = await evaluator.EvaluateAsync(
            kiosk,
            new(DateTimeOffset.UtcNow, CheckCustomerSession: false),
            cancellationToken);
        return ApiResult<KioskSalesReadinessResult>.Success(new KioskSalesReadinessResult(
            kiosk.Id,
            decision.CanExposeCatalog,
            decision.CanPlaceOrder,
            decision.CanOpenPayment,
            decision.Blockers.Select(blocker => new KioskSalesReadinessBlockerResult(
                blocker.Code.ToString(), blocker.Scope.ToString())).ToArray(),
            decision.EvaluatedAt,
            decision.EvidenceValidUntil));
    }
}

public sealed record KioskSalesReadinessResult(
    Guid KioskId,
    bool CanExposeCatalog,
    bool CanPlaceOrder,
    bool CanOpenPayment,
    IReadOnlyList<KioskSalesReadinessBlockerResult> Blockers,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset? EvidenceValidUntil);

public sealed record KioskSalesReadinessBlockerResult(string Code, string Scope);
