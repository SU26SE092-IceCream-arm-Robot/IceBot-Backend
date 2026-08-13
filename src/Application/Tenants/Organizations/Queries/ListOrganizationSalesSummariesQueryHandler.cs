using Application.Shared.Wrappers;
using Application.Tenants.Organizations.Abstractions;
using Application.Tenants.Organizations.ReadModels;
using Application.Tenants.Organizations.Results;

namespace Application.Tenants.Organizations.Queries;

public sealed class ListOrganizationSalesSummariesQueryHandler
{
    private const int MaximumRangeDays = 366;
    private readonly IOrganizationSalesSummaryStore _salesSummaryStore;

    public ListOrganizationSalesSummariesQueryHandler(IOrganizationSalesSummaryStore salesSummaryStore)
    {
        _salesSummaryStore = salesSummaryStore;
    }

    public async Task<PagedResult<OrganizationSalesSummaryResult>> HandleAsync(
        ListOrganizationSalesSummariesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        if (!query.UserContext.IsSystemAdmin)
        {
            return PagedResult<OrganizationSalesSummaryResult>.Forbidden(
                "Only system administrators can access organization sales summaries.", pageNumber, pageSize);
        }

        if (!TryCreateReadRequest(query, pageNumber, pageSize, out var request, out var validationMessage))
        {
            return PagedResult<OrganizationSalesSummaryResult>.Fail(validationMessage!, 400, pageNumber, pageSize);
        }

        var totalCount = await _salesSummaryStore.CountAsync(request!, cancellationToken);
        var summaries = await _salesSummaryStore.ListAsync(request!, cancellationToken);
        return PagedResult<OrganizationSalesSummaryResult>.Success(
            summaries.Select(summary => new OrganizationSalesSummaryResult
            {
                OrganizationId = summary.OrganizationId,
                OrganizationCode = summary.OrganizationCode,
                OrganizationName = summary.OrganizationName,
                OrganizationStatus = summary.OrganizationStatus,
                Currency = summary.Currency,
                PaidOrderCount = summary.PaidOrderCount,
                GrossCollectedAmount = summary.GrossCollectedAmount,
                ProcessedRefundAmount = summary.ProcessedRefundAmount
            }), totalCount, pageNumber, pageSize);
    }

    private static bool TryCreateReadRequest(ListOrganizationSalesSummariesQuery query, int pageNumber, int pageSize,
        out OrganizationSalesSummaryReadRequest? request, out string? validationMessage)
    {
        request = null;
        validationMessage = null;
        if (!query.From.HasValue || !query.To.HasValue)
        {
            validationMessage = "Both from and to are required UTC timestamps.";
            return false;
        }

        if (query.From.Value.Offset != TimeSpan.Zero || query.To.Value.Offset != TimeSpan.Zero)
        {
            validationMessage = "from and to must use UTC timestamps.";
            return false;
        }

        var from = query.From.Value;
        var to = query.To.Value;
        if (from >= to)
        {
            validationMessage = "from must be earlier than to.";
            return false;
        }

        if (to - from > TimeSpan.FromDays(MaximumRangeDays))
        {
            validationMessage = $"The requested time range cannot exceed {MaximumRangeDays} days.";
            return false;
        }

        request = new OrganizationSalesSummaryReadRequest(from, to, query.OrganizationId,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), pageNumber, pageSize);
        return true;
    }
}
