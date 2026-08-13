namespace Application.Tenants.Organizations.Results;

public sealed class OrganizationSalesSummaryResult
{
    public Guid OrganizationId { get; init; }
    public string OrganizationCode { get; init; } = null!;
    public string OrganizationName { get; init; } = null!;
    public string OrganizationStatus { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public int PaidOrderCount { get; init; }
    public decimal GrossCollectedAmount { get; init; }
    public decimal ProcessedRefundAmount { get; init; }
    public decimal NetCollectedAmount => GrossCollectedAmount - ProcessedRefundAmount;
}
