using Application.Shared.Wrappers;

namespace Application.Orders.PlaceOrder;

public static class OrderErrors
{
    public static readonly ApiBusinessErrorDefinition IdempotencyKeyInvalid = new(
        "ORDER.IDEMPOTENCY_KEY_INVALID", 400, "Idempotency-Key is required and invalid.");
    public static readonly ApiBusinessErrorDefinition IdempotencyConflict = new(
        "ORDER.IDEMPOTENCY_CONFLICT", 409, "Idempotency key was already used for a different order request.");
    public static readonly ApiBusinessErrorDefinition ClientOrderIdConflict = new(
        "ORDER.CLIENT_ORDER_ID_CONFLICT", 409, "Client order id was already used for a different order request.");
    public static readonly ApiBusinessErrorDefinition ClientTotalMismatch = new(
        "ORDER.CLIENT_TOTAL_MISMATCH", 409, "Client total does not match calculated total.");
    public static readonly ApiBusinessErrorDefinition CurrencyMismatch = new(
        "ORDER.CURRENCY_MISMATCH", 400, "All order items must use the same currency.");
    public static readonly ApiBusinessErrorDefinition OptionSelectionInvalid = new(
        "ORDER.OPTION_SELECTION_INVALID", 409, "Selected product options are invalid.");
    public static readonly ApiBusinessErrorDefinition PackagedOptionUnsupported = new(
        "ORDER.PACKAGED_OPTION_UNSUPPORTED", 409, "Packaged menu items cannot use production-affecting options.");

    public static IReadOnlyList<ApiBusinessErrorDefinition> All { get; } =
        [IdempotencyKeyInvalid, IdempotencyConflict, ClientOrderIdConflict, ClientTotalMismatch,
         CurrencyMismatch, OptionSelectionInvalid, PackagedOptionUnsupported];
}
