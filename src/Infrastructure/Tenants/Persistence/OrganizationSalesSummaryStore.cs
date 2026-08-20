using Application.Tenants.Organizations.Abstractions;
using Application.Tenants.Organizations.ReadModels;
using Domain.Common.Enums;
using Domain.Payments.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class OrganizationSalesSummaryStore : IOrganizationSalesSummaryStore
{
    private readonly IceBotDbContext _dbContext;

    public OrganizationSalesSummaryStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CountAsync(OrganizationSalesSummaryReadRequest request, CancellationToken cancellationToken = default) =>
        BuildRows(request).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<OrganizationSalesSummaryReadModel>> ListAsync(
        OrganizationSalesSummaryReadRequest request,
        CancellationToken cancellationToken = default) =>
        (await BuildRows(request).OrderBy(summary => summary.OrganizationCode).ThenBy(summary => summary.Currency)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken))
            .Select(summary => new OrganizationSalesSummaryReadModel(
                summary.OrganizationId,
                summary.OrganizationCode,
                summary.OrganizationName,
                summary.OrganizationStatus.ToString(),
                summary.Currency,
                summary.PaidOrderCount,
                summary.GrossCollectedAmount,
                summary.ProcessedRefundAmount))
            .ToArray();

    private IQueryable<OrganizationSalesSummaryDatabaseRow> BuildRows(OrganizationSalesSummaryReadRequest request)
    {
        var grossCollections = _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .Where(transaction => transaction.SettlementDisposition == PaymentSettlementDisposition.Primary &&
                transaction.PaidAt != null && transaction.PaidAt >= request.From && transaction.PaidAt < request.To &&
                transaction.PaidAmount != null && transaction.Order.OrganizationId != null && transaction.Order.DeletedAt == null)
            .Select(transaction => new
            {
                OrganizationId = transaction.Order.OrganizationId!.Value,
                transaction.Currency,
                GrossCollectedAmount = transaction.PaidAmount!.Value,
                ProcessedRefundAmount = 0m,
                PaidOrderCount = 1
            });

        var processedRefunds = _dbContext.Refunds.WhereNotDeleted().AsNoTracking()
            .Where(refund => refund.Status == RefundStatus.Processed && refund.ProcessedAt != null &&
                refund.CompensationMethod == RefundCompensationMethod.FullMoneyRefund &&
                refund.ProcessedAt >= request.From && refund.ProcessedAt < request.To &&
                refund.PaymentTransaction.SettlementDisposition == PaymentSettlementDisposition.Primary &&
                refund.PaymentTransaction.Order.OrganizationId != null && refund.PaymentTransaction.Order.DeletedAt == null)
            .Select(refund => new
            {
                OrganizationId = refund.PaymentTransaction.Order.OrganizationId!.Value,
                refund.Currency,
                GrossCollectedAmount = 0m,
                ProcessedRefundAmount = refund.Amount,
                PaidOrderCount = 0
            });

        var financialSummary = grossCollections.Concat(processedRefunds)
            .GroupBy(row => new { row.OrganizationId, row.Currency })
            .Select(group => new
            {
                group.Key.OrganizationId,
                group.Key.Currency,
                GrossCollectedAmount = group.Sum(row => row.GrossCollectedAmount),
                ProcessedRefundAmount = group.Sum(row => row.ProcessedRefundAmount),
                PaidOrderCount = group.Sum(row => row.PaidOrderCount)
            });

        var organizations = _dbContext.Organizations.WhereNotDeleted().AsNoTracking();
        if (request.OrganizationId.HasValue)
        {
            organizations = organizations.Where(organization => organization.Id == request.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            organizations = organizations.Where(organization => organization.Code.ToLower().Contains(search) ||
                organization.Name.ToLower().Contains(search));
        }

        return from summary in financialSummary
               join organization in organizations on summary.OrganizationId equals organization.Id
               select new OrganizationSalesSummaryDatabaseRow
               {
                   OrganizationId = organization.Id,
                   OrganizationCode = organization.Code,
                   OrganizationName = organization.Name,
                   OrganizationStatus = organization.Status,
                   Currency = summary.Currency,
                   PaidOrderCount = summary.PaidOrderCount,
                   GrossCollectedAmount = summary.GrossCollectedAmount,
                   ProcessedRefundAmount = summary.ProcessedRefundAmount
               };
    }

    private sealed class OrganizationSalesSummaryDatabaseRow
    {
        public Guid OrganizationId { get; init; }
        public string OrganizationCode { get; init; } = null!;
        public string OrganizationName { get; init; } = null!;
        public EntityStatus OrganizationStatus { get; init; }
        public string Currency { get; init; } = null!;
        public int PaidOrderCount { get; init; }
        public decimal GrossCollectedAmount { get; init; }
        public decimal ProcessedRefundAmount { get; init; }
    }
}
