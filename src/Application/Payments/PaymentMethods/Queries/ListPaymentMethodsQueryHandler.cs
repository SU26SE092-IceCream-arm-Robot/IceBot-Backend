using Application.Payments.Abstractions;
using Application.Payments.PaymentMethods.DTOs;
using Application.Payments.PaymentMethods.Mapping;
using Application.Shared.Wrappers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.PaymentMethods.Queries;

public sealed class ListPaymentMethodsQueryHandler
{
    private readonly IPaymentStore _paymentStore;

    public ListPaymentMethodsQueryHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<IEnumerable<PaymentMethodResult>>> HandleAsync(
        ListPaymentMethodsQuery query,
        CancellationToken cancellationToken = default)
    {
        var methods = await _paymentStore.ListPaymentMethodsAsync(cancellationToken);
        
        var queryable = methods.AsEnumerable();
        if (query.ActiveOnly == true)
        {
            queryable = queryable.Where(x => x.IsActive);
        }

        var data = queryable
            .OrderBy(x => x.Name)
            .Select(PaymentMethodResultMapper.ToResult)
            .ToList();

        var message = query.ActiveOnly == true 
            ? "Active payment methods retrieved successfully." 
            : "Payment methods retrieved successfully.";

        return ApiResult<IEnumerable<PaymentMethodResult>>.Success(data, message);
    }
}
