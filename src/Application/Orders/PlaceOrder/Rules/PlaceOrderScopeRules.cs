namespace Application.Orders.PlaceOrder.Rules;

internal static class PlaceOrderScopeRules
{
    public static bool MatchesScope(Guid? entityScopeId, Guid? kioskScopeId)
    {
        return entityScopeId is null || entityScopeId == kioskScopeId;
    }

    public static bool IsWithinEffectiveWindow(
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        DateTimeOffset now)
    {
        return (effectiveFrom is null || effectiveFrom <= now) &&
               (effectiveTo is null || effectiveTo >= now);
    }
}
