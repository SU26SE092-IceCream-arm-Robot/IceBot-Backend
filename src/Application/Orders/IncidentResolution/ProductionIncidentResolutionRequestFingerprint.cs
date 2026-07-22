using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Application.Orders.IncidentResolution;

internal static class ProductionIncidentResolutionRequestFingerprint
{
    public static string Compute(SelectProductionIncidentResolutionCommand command)
    {
        var payload = string.Join('\n',
            ((int)command.Resolution).ToString(CultureInfo.InvariantCulture),
            command.Reason?.Trim() ?? string.Empty,
            command.PaymentTransactionId?.ToString("D") ?? string.Empty,
            command.VoucherCode?.Trim() ?? string.Empty,
            command.VoucherValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            command.AcknowledgeFullOrderCompensation ? "1" : "0");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
