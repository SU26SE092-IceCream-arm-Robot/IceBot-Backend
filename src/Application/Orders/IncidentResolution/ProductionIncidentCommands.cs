using Application.EdgeIntegration.Dispatch.Commands;
using Application.Orders.IncidentResolution.Abstractions;
using Application.Orders.Management.Commands;
using Application.Payments.Refunds.Commands;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Orders.Incidents;
using Domain.ProductionExecution.Enums;

namespace Application.Orders.IncidentResolution;

public sealed class OpenProductionIncidentCommandHandler(IProductionIncidentStore store)
{
    public Task<ApiResult<ProductionIncidentResult>> HandleAsync(OpenProductionIncidentCommand command, CancellationToken ct = default) =>
        store.ExecuteInTransactionAsync(async innerCt =>
        {
            await store.AcquireSourceLockAsync(command.SourceCommandId, command.SourceProductionJobId, innerCt);
            var order = await store.GetOrderAsync(command.OrderId, innerCt);
            if (order is null || !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersManage,
                    command.UserContext, order.OrganizationId, order.StoreId, order.KioskId))
                return ApiResult<ProductionIncidentResult>.Fail("Order not found.", 404);
            var item = order.OrderItems.SingleOrDefault(x => x.Id == command.OrderItemId);
            if (item is null)
                return ApiResult<ProductionIncidentResult>.Fail("Order item not found.", 404);
            var existing = await store.GetBySourceAsync(command.SourceCommandId, command.SourceProductionJobId, innerCt);
            if (existing is not null)
            {
                if (existing.OrderId != order.Id || existing.OrderItemId != item.Id ||
                    existing.ProductionUnitNo != command.ProductionUnitNo ||
                    existing.ProductionUnitQuantity != command.ProductionUnitQuantity)
                    return ApiResult<ProductionIncidentResult>.Fail(
                        "Production execution evidence does not match the requested unit range.", 409);
                if (existing.InspectionOutcome != ProductionInspectionOutcome.Defective)
                {
                    try
                    {
                        existing.RecordInspection(ProductionInspectionOutcome.Defective,
                            command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow);
                    }
                    catch (DomainRuleException ex)
                    {
                        return ApiResult<ProductionIncidentResult>.Fail(ex.Message, 409);
                    }
                    await store.SaveChangesAsync(innerCt);
                }
                return ApiResult<ProductionIncidentResult>.Success(
                    ProductionIncidentResultMapper.Map(existing), "Existing production incident returned.");
            }
            var record = await store.GetProductionRecordAsync(command.SourceCommandId, command.SourceProductionJobId, innerCt);
            if (record is null || record.OrderItemId != item.Id ||
                record.ProductionUnitNo != command.ProductionUnitNo || record.ProductionUnitQuantity != command.ProductionUnitQuantity)
                return ApiResult<ProductionIncidentResult>.Fail("Production execution evidence does not match the requested unit range.", 409);
            var now = DateTimeOffset.UtcNow;
            var incident = ProductionIncident.OpenFromExecution(order.OrganizationId, order.StoreId, order.KioskId,
                order.Id, item.Id, command.SourceCommandId, command.SourceProductionJobId,
                command.ProductionUnitNo, command.ProductionUnitQuantity,
                ProductionIncidentTrigger.DefectiveOutputReported, record.PhysicalOutputState,
                order.OrderNumber, item.ProductNameSnapshot, item.ProductVariantNameSnapshot,
                record.ErrorCode, record.ErrorMessage, now, command.UserContext.AccountId);
            incident.RecordInspection(ProductionInspectionOutcome.Defective, command.UserContext.AccountId, command.Reason, now);
            await store.AddAsync(incident, innerCt);
            await store.SaveChangesAsync(innerCt);
            return ApiResult<ProductionIncidentResult>.Success(ProductionIncidentResultMapper.Map(incident), "Production incident opened.", 201);
        }, ct);
}

public sealed class RecordProductionInspectionCommandHandler(IProductionIncidentStore store)
{
    public Task<ApiResult<ProductionIncidentResult>> HandleAsync(RecordProductionInspectionCommand command, CancellationToken ct = default) =>
        MutateAsync(store, command.OrderId, command.IncidentId, command.UserContext,
            incident => incident.RecordInspection(command.Outcome, command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow), ct);

    internal static Task<ApiResult<ProductionIncidentResult>> MutateAsync(IProductionIncidentStore store, Guid orderId, Guid incidentId,
        Application.Identity.Tokens.Claims.CurrentUserContext user, Action<ProductionIncident> mutate, CancellationToken ct) =>
        store.ExecuteInTransactionAsync(async innerCt =>
        {
            await store.AcquireIncidentLockAsync(incidentId, innerCt);
            var incident = await store.GetByIdAsync(incidentId, true, innerCt);
            if (incident is null || incident.OrderId != orderId || !GetProductionIncidentQueryHandler.CanManage(incident, user))
                return ApiResult<ProductionIncidentResult>.Fail("Production incident not found.", 404);
            try { mutate(incident); }
            catch (DomainRuleException ex) { return ApiResult<ProductionIncidentResult>.Fail(ex.Message, 409); }
            await store.SaveChangesAsync(innerCt);
            return ApiResult<ProductionIncidentResult>.Success(ProductionIncidentResultMapper.Map(incident));
        }, ct);
}

public sealed class SelectProductionIncidentResolutionCommandHandler(
    IProductionIncidentStore store,
    RequestOrderItemProductionRemakeCommandHandler remakeHandler,
    MarkOrderRefundRequiredCommandHandler refundRequiredHandler,
    RequestRefundCommandHandler refundHandler)
{
    public async Task<ApiResult<ProductionIncidentResult>> HandleAsync(SelectProductionIncidentResolutionCommand command, CancellationToken ct = default)
    {
        var requestFingerprint = ProductionIncidentResolutionRequestFingerprint.Compute(command);
        var selected = await RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
            command.UserContext, incident => incident.SelectResolution(command.Resolution, command.ResolutionRequestId, requestFingerprint,
                command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow), ct);
        if (!selected.Succeeded) return selected;
        if (selected.Data!.Status is nameof(ProductionIncidentStatus.ResolutionInProgress) or
            nameof(ProductionIncidentStatus.Resolved))
            return selected;

        if (command.Resolution is ProductionIncidentResolution.DeliverExistingOutput or
            ProductionIncidentResolution.DiscardProduct or ProductionIncidentResolution.NoAction)
            return await RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
                command.UserContext, incident =>
                {
                    var now = DateTimeOffset.UtcNow;
                    incident.RecordResolutionAction(null, null, command.UserContext.AccountId, command.Reason, now);
                    incident.Resolve(command.UserContext.AccountId, command.Reason, now);
                }, ct);

        if (command.Resolution == ProductionIncidentResolution.AwaitTechnicalReview)
            return await RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
                command.UserContext, incident => incident.RecordResolutionAction(
                    null, null, command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow), ct);

        if (command.Resolution == ProductionIncidentResolution.RequestRemake)
        {
            var incident = selected.Data!;
            var remake = await remakeHandler.HandleAsync(new RequestOrderItemProductionRemakeCommand(
                command.OrderId, incident.OrderItemId, command.ResolutionRequestId,
                incident.ProductionUnitNo, incident.ProductionUnitQuantity, command.Reason,
                command.UserContext, command.IncidentId), ct);
            if (!remake.Succeeded)
                return ApiResult<ProductionIncidentResult>.Fail(remake.Message ?? "Production remake could not be requested.", remake.StatusCode);
            return await RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
                command.UserContext, current => current.RecordResolutionAction(
                    remake.Data!.EdgeCommandId, null, command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow), ct);
        }

        if (!command.AcknowledgeFullOrderCompensation)
            return ApiResult<ProductionIncidentResult>.Fail(
                "Refund and voucher resolutions require acknowledgement that V1 compensates the full order.", 409);
        var flagged = await refundRequiredHandler.HandleAsync(new MarkOrderRefundRequiredCommand
        {
            OrderId = command.OrderId, UserContext = command.UserContext, Reason = command.Reason
        }, ct);
        if (!flagged.Succeeded)
            return ApiResult<ProductionIncidentResult>.Fail(flagged.Message ?? "Order could not be flagged for refund.", flagged.StatusCode);
        var refund = await refundHandler.HandleAsync(new RequestRefundCommand
        {
            OrderId = command.OrderId,
            PaymentTransactionId = command.PaymentTransactionId,
            UserContext = command.UserContext,
            RefundMethod = command.Resolution == ProductionIncidentResolution.IssueVoucher ? "Voucher" : "FullMoneyRefund",
            Reason = command.Reason,
            VoucherCode = command.VoucherCode,
            VoucherValue = command.VoucherValue,
            IdempotencyKey = $"production-incident-{command.ResolutionRequestId:N}"
        }, ct);
        if (!refund.Succeeded)
            return ApiResult<ProductionIncidentResult>.Fail(refund.Message ?? "Refund could not be requested.", refund.StatusCode);
        return await RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
            command.UserContext, current => current.RecordResolutionAction(
                null, refund.Data!.Id, command.UserContext.AccountId, command.Reason, DateTimeOffset.UtcNow), ct);
    }
}

public sealed class CompleteProductionIncidentCommandHandler(IProductionIncidentStore store)
{
    public Task<ApiResult<ProductionIncidentResult>> HandleAsync(CompleteProductionIncidentCommand command, CancellationToken ct = default) =>
        RecordProductionInspectionCommandHandler.MutateAsync(store, command.OrderId, command.IncidentId,
            command.UserContext, incident => incident.Resolve(command.UserContext.AccountId, command.Notes, DateTimeOffset.UtcNow), ct);
}
