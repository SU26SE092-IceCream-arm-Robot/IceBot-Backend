using Application.Payments.Abstractions;
using Application.Payments.PaymentMethods.DTOs;
using Application.Payments.PaymentMethods.Interfaces;
using Application.Shared.Wrappers;

namespace Application.Payments.PaymentMethods.Services
{
    public class ManagePaymentMethodService : IManagePaymentMethodService
    {
        private readonly IPaymentStore _paymentStore;

        public ManagePaymentMethodService(IPaymentStore paymentStore)
        {
            _paymentStore = paymentStore;
        }

        public async Task<ApiResult<IEnumerable<PaymentMethodResult>>> GetAllAsync()
        {
            var methods = await _paymentStore.ListPaymentMethodsAsync();
            var data = methods
                .OrderBy(x => x.Name)
                .Select(x => new PaymentMethodResult
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToList();

            return ApiResult<IEnumerable<PaymentMethodResult>>.Success(data, "Payment methods retrieved successfully.");
        }

        public async Task<ApiResult<IEnumerable<PaymentMethodResult>>> GetActiveAsync()
        {
            var methods = await _paymentStore.ListPaymentMethodsAsync();
            var data = methods
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new PaymentMethodResult
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToList();

            return ApiResult<IEnumerable<PaymentMethodResult>>.Success(data, "Active payment methods retrieved successfully.");
        }

        public async Task<ApiResult<bool>> SetStatusAsync(long id, bool isActive)
        {
            var methods = await _paymentStore.ListPaymentMethodsAsync();
            var method = methods.FirstOrDefault(x => x.Id == id);
            if (method == null)
            {
                return ApiResult<bool>.Fail("Payment method not found.", 404);
            }

            if (method.IsActive == isActive)
            {
                var message = isActive ? "Payment method already active." : "Payment method already inactive.";
                return ApiResult<bool>.Success(true, message, 200);
            }

            method.IsActive = isActive;
            method.UpdatedAt = DateTimeOffset.UtcNow;
            await _paymentStore.SaveChangesAsync();

            var successMessage = isActive ? "Payment method activated." : "Payment method deactivated.";
            return ApiResult<bool>.Success(true, successMessage, 200);
        }
    }
}
