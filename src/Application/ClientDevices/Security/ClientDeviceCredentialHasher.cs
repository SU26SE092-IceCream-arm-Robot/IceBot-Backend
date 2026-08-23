using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Application.ClientDevices.Security;

public sealed class ClientDeviceCredentialHasher(IOptions<ClientDeviceSecurityOptions> options)
{
    private readonly ClientDeviceSecurityOptions _options = options.Value;

    public string CurrentHashKeyVersion => _options.CurrentHashKeyVersion;

    public byte[] ComputeCurrent(string credential) => Compute(credential, CurrentHashKeyVersion);

    public byte[] Compute(string credential, string keyVersion)
    {
        if (!TryDecodeCredential(credential, out var rawCredential))
            throw new ArgumentException("Client device credential must be a base64-encoded 256-bit secret.", nameof(credential));
        if (!_options.HashKeys.TryGetValue(keyVersion, out var key) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException($"Client device credential hash key '{keyVersion}' is unavailable.");

        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), rawCredential);
    }

    public bool Matches(string credential, byte[] expectedHash, string keyVersion)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(Compute(credential, keyVersion), expectedHash);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TryDecodeCredential(string? credential, out byte[] rawCredential)
    {
        rawCredential = [];
        if (string.IsNullOrWhiteSpace(credential))
            return false;

        try
        {
            rawCredential = Convert.FromBase64String(credential.Trim());
            return rawCredential.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
