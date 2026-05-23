using System.ComponentModel.DataAnnotations;

namespace Application.Payments.PaymentMethods.DTOs
{
    public class PaymentMethodStatusUpdateRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
