namespace Application.Payments.PaymentMethods.DTOs
{
    public class PaymentMethodResult
    {
        public long Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
