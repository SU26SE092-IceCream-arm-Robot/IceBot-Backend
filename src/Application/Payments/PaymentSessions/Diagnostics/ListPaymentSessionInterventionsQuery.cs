using Application.Identity.Tokens.Claims;

namespace Application.Payments.PaymentSessions.Diagnostics;

public sealed record ListPaymentSessionInterventionsQuery(
    CurrentUserContext UserContext,
    string? Provider,
    string? InterventionCode,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    int PageNumber = 1,
    int PageSize = 20);
