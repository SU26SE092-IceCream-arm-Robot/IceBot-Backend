using Application.Payments.PaymentMethods.DTOs;
using Application.Shared.Wrappers;

namespace Application.Payments.PaymentMethods.Interfaces
{
    public interface IManagePaymentMethodService
    {
        Task<ApiResult<IEnumerable<PaymentMethodResult>>> GetAllAsync();
        Task<ApiResult<IEnumerable<PaymentMethodResult>>> GetActiveAsync();
        Task<ApiResult<bool>> SetStatusAsync(long id, bool isActive);
    }
}
