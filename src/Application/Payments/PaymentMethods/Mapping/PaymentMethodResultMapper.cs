using Application.Payments.PaymentMethods.DTOs;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentMethods.Mapping;

internal static class PaymentMethodResultMapper
{
    public static PaymentMethodResult ToResult(PaymentMethod method)
    {
        return new PaymentMethodResult
        {
            Id = method.Id,
            Code = method.Code,
            Name = method.Name,
            Description = method.Description,
            IsActive = method.IsActive
        };
    }
}
