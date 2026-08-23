using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.Shared.Wrappers;
using Application.SalesCatalog.RuntimeMenus.Services;
using Application.SalesCatalog.RuntimeMenus.Support;
using Application.SalesCatalog.Admission.Services;
using Application.SalesCatalog.Admission.Abstractions;
using Domain.Catalog.Enums;

namespace Application.SalesCatalog.RuntimeMenus.Queries;

public sealed class GetKioskRuntimeMenuQueryHandler
{
    private readonly IMenuStore _menus;
    private readonly RuntimeMenuProjectionBuilder _projectionBuilder;
    private readonly IRuntimeMenuProjectionCache _cache;
    private readonly KioskSalesAdmissionEvaluator _kioskAdmission;
    private readonly IMenuItemOperationalAdmissionEvaluator _itemAdmission;

    public GetKioskRuntimeMenuQueryHandler(
        IMenuStore menus,
        RuntimeMenuProjectionBuilder projectionBuilder,
        IRuntimeMenuProjectionCache cache,
        KioskSalesAdmissionEvaluator kioskAdmission,
        IMenuItemOperationalAdmissionEvaluator itemAdmission)
    {
        _menus = menus;
        _projectionBuilder = projectionBuilder;
        _cache = cache;
        _kioskAdmission = kioskAdmission;
        _itemAdmission = itemAdmission;
    }

    public async Task<ApiResult<RuntimeMenuResult>> HandleAsync(
        GetKioskRuntimeMenuQuery query,
        CancellationToken cancellationToken = default)
    {
        var kioskId = query.KioskId;
        var kiosk = await _menus.GetKioskByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<RuntimeMenuResult>.Fail("Kiosk not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        var admission = await _kioskAdmission.EvaluateAsync(kiosk, new(now), cancellationToken);
        if (!admission.CanExposeCatalog)
        {
            return ApiResult<RuntimeMenuResult>.Fail(admission.ToDisplayMessage()!, 409);
        }

        var projection = await _cache.GetOrCreateAsync(
            kiosk.Id,
            ct => _projectionBuilder.BuildAsync(kiosk, ct),
            cancellationToken);

        var availableItems = new List<RuntimeMenuItemResult>();
        foreach (var item in projection.Items)
        {
            var itemDecision = await _itemAdmission.EvaluateAsync(
                kiosk,
                item.MenuItemId,
                quantity: 1,
                selectedOptionIngredients: null,
                now,
                cancellationToken);
            if (itemDecision.CanSell)
            {
                var itemWithLiveOptionPolicy = ApplyProductionOptionPolicy(item, itemDecision.SupportedProductionOptionCodes);
                if (HasSatisfiableOptionGroups(itemWithLiveOptionPolicy))
                {
                    availableItems.Add(itemWithLiveOptionPolicy);
                }
            }
        }

        var visibleItems = admission.CanPlaceOrder ? availableItems : [];

        var result = new RuntimeMenuResult
        {
            SnapshotId = Guid.CreateVersion7(),
            Revision = RuntimeMenuRevision.Compute(kiosk.Id, visibleItems),
            KioskId = kiosk.Id,
            GeneratedAt = now,
            ExpiresAt = projection.ValidUntil,
            Items = visibleItems,
            Admission = new RuntimeMenuAdmissionResult
            {
                CanPlaceOrder = admission.CanPlaceOrder,
                CanOpenPayment = admission.CanOpenPayment,
                EvidenceValidUntil = admission.EvidenceValidUntil,
                Blockers = admission.Blockers.Select(blocker => new RuntimeMenuAdmissionBlockerResult
                {
                    Code = blocker.Code.ToString(),
                    Scope = blocker.Scope.ToString()
                }).ToList()
            }
        };

        return ApiResult<RuntimeMenuResult>.Success(result);
    }

    private static RuntimeMenuItemResult ApplyProductionOptionPolicy(
        RuntimeMenuItemResult item,
        IReadOnlySet<string> supportedProductionOptionCodes)
    {
        return new RuntimeMenuItemResult
        {
            MenuId = item.MenuId,
            MenuItemId = item.MenuItemId,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            RecipeId = item.RecipeId,
            MenuItemCode = item.MenuItemCode,
            ProductCode = item.ProductCode,
            ProductVariantCode = item.ProductVariantCode,
            DisplayName = item.DisplayName,
            Description = item.Description,
            SizeCode = item.SizeCode,
            Price = item.Price,
            DiscountAmount = item.DiscountAmount,
            FinalPrice = item.FinalPrice,
            Currency = item.Currency,
            PreparationTimeSeconds = item.PreparationTimeSeconds,
            Image = item.Image,
            RecipeVersion = item.RecipeVersion,
            OptionGroups = item.OptionGroups.Select(group => new RuntimeMenuOptionGroupResult
            {
                OptionGroupId = group.OptionGroupId,
                Code = group.Code,
                Name = group.Name,
                SelectionType = group.SelectionType,
                MinSelections = group.MinSelections,
                MaxSelections = group.MaxSelections,
                IsRequired = group.IsRequired,
                Options = group.Options
                    .Where(option =>
                        option.ExecutionImpact != ProductOptionExecutionImpact.ProductionAffecting.ToString() ||
                        supportedProductionOptionCodes.Contains(option.Code))
                    .Select(option => new RuntimeMenuProductOptionResult
                    {
                        ProductOptionId = option.ProductOptionId,
                        Code = option.Code,
                        Name = option.Name,
                        Description = option.Description,
                        PriceDelta = option.PriceDelta,
                        Currency = option.Currency,
                        IsDefault = option.IsDefault,
                        ExecutionImpact = option.ExecutionImpact
                    }).ToList()
            }).ToList()
        };
    }

    private static bool HasSatisfiableOptionGroups(RuntimeMenuItemResult item) =>
        item.OptionGroups.All(group =>
        {
            var minimum = group.IsRequired ? Math.Max(1, group.MinSelections) : group.MinSelections;
            var maximum = string.Equals(group.SelectionType, "Single", StringComparison.Ordinal)
                ? 1
                : group.MaxSelections;
            return group.Options.Count >= minimum && (maximum <= 0 || minimum <= maximum);
        });
}
