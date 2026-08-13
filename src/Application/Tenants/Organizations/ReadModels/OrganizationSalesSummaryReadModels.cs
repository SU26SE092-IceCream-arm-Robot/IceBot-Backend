namespace Application.Tenants.Organizations.ReadModels;

public sealed record OrganizationSalesSummaryReadRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? OrganizationId,
    string? Search,
    int PageNumber,
    int PageSize);

public sealed record OrganizationSalesSummaryReadModel(
    Guid OrganizationId,
    string OrganizationCode,
    string OrganizationName,
    string OrganizationStatus,
    string Currency,
    int PaidOrderCount,
    decimal GrossCollectedAmount,
    decimal ProcessedRefundAmount);
