using Application.Payments.Abstractions;
using Application.Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.PaymentMethods.Commands;

public sealed class SetPaymentMethodStatusCommandHandler
{
    private readonly IPaymentStore _paymentStore;

    public SetPaymentMethodStatusCommandHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        SetPaymentMethodStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var methods = await _paymentStore.ListPaymentMethodsAsync(cancellationToken);
        var method = methods.FirstOrDefault(x => x.Id == command.Id);
        if (method == null)
        {
            return ApiResult<bool>.Fail("Payment method not found.", 404);
        }

        if (method.IsActive == command.IsActive)
        {
            var message = command.IsActive ? "Payment method already active." : "Payment method already inactive.";
            return ApiResult<bool>.Success(true, message, 200);
        }

        method.IsActive = command.IsActive;
        method.UpdatedAt = DateTimeOffset.UtcNow;
        await _paymentStore.SaveChangesAsync(cancellationToken);

        var successMessage = command.IsActive ? "Payment method activated." : "Payment method deactivated.";
        return ApiResult<bool>.Success(true, successMessage, 200);
    }
}
