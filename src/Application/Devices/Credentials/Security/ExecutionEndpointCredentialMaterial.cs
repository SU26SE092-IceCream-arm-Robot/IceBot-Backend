using System.Security.Cryptography;
using System.Text;

namespace Application.Devices.Credentials.Security;

public sealed record ExecutionEndpointCredentialMaterial(string Fingerprint, string? PublicKeyPem);

public static class ExecutionEndpointCredentialMaterialFactory
{
    public static ExecutionEndpointCredentialMaterial FromCertificateFingerprint(string fingerprint)
    {
        var normalized = new string((fingerprint ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character) && character != ':')
            .ToArray())
            .ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !char.IsAsciiHexDigit(character)))
            throw new ArgumentException("Client certificate fingerprint must be a SHA-256 hexadecimal value.");
        return new ExecutionEndpointCredentialMaterial(normalized, null);
    }

    public static ExecutionEndpointCredentialMaterial FromEcdsaPublicKey(string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("ECDSA public key PEM is required.");

        using var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(publicKeyPem);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("ECDSA public key PEM is invalid.", ex);
        }

        if (ecdsa.KeySize != 256)
            throw new ArgumentException("Low-cost execution endpoints require an ECDSA P-256 public key.");

        var curveOid = ecdsa.ExportParameters(false).Curve.Oid.Value;
        if (!string.Equals(curveOid, "1.2.840.10045.3.1.7", StringComparison.Ordinal))
            throw new ArgumentException("Low-cost execution endpoints require the NIST P-256 curve.");

        var publicKeyDer = ecdsa.ExportSubjectPublicKeyInfo();
        var canonicalPem = ecdsa.ExportSubjectPublicKeyInfoPem();
        var fingerprint = Convert.ToHexString(SHA256.HashData(publicKeyDer)).ToLowerInvariant();
        return new ExecutionEndpointCredentialMaterial(fingerprint, canonicalPem);
    }
}
