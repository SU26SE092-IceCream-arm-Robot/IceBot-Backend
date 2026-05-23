namespace Infrastructure.Payments.Options;

public class PayOsOptions
{
    public const string SectionName = "PayOS";

    public required string ClientId { get; set; }

    public required string ApiKey { get; set; }

    public required string ChecksumKey { get; set; }

    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";

    public required string ReturnUrl { get; set; }

    public required string CancelUrl { get; set; }

    public int ExpireMinutes { get; set; } = 15;
}
