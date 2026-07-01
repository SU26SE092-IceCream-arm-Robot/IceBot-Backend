using Domain.Devices.ExecutionEndpoints;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Microsoft.Extensions.Options;
using WebAPI.Middlewares;

namespace WebAPI.Configuration.Security;

public sealed record ExecutionEndpointRequestAuthenticationResult(
    bool Succeeded,
    string Message,
    KioskExecutionEndpoint? Endpoint)
{
    public static ExecutionEndpointRequestAuthenticationResult Success(KioskExecutionEndpoint? endpoint = null) =>
        new(true, "Authenticated.", endpoint);
    public static ExecutionEndpointRequestAuthenticationResult Fail(string message) => new(false, message, null);
}

public sealed class ExecutionEndpointRequestAuthenticator
{
    public const string TimestampHeader = "X-Execution-Timestamp";
    public const string NonceHeader = "X-Execution-Nonce";
    public const string SignatureHeader = "X-Execution-Signature";

    private readonly IExecutionEndpointTransportAuthStore _store;
    private readonly ExecutionEndpointSecurityOptions _options;

    public ExecutionEndpointRequestAuthenticator(
        IExecutionEndpointTransportAuthStore store,
        IOptions<ExecutionEndpointSecurityOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<ExecutionEndpointRequestAuthenticationResult> AuthenticateAsync(
        HttpContext context,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        if (!context.Request.IsHttps)
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution endpoint requests require HTTPS.");

        if (endpointId == Guid.Empty)
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution endpoint id is required.");

        var endpoint = await _store.GetEndpointAsync(endpointId, cancellationToken);
        if (endpoint is null ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding is null ||
            endpoint.CredentialBinding.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution endpoint authentication failed.");
        }

        var authentication = endpoint.AuthenticationMode switch
        {
            ExecutionEndpointAuthenticationMode.MutualTls =>
                await AuthenticateMutualTlsAsync(context, endpoint.CredentialBinding, cancellationToken),
            ExecutionEndpointAuthenticationMode.SignedCommandTls =>
                await AuthenticateSignedRequestAsync(context, endpoint, cancellationToken),
            _ => ExecutionEndpointRequestAuthenticationResult.Fail("Execution endpoint authentication mode is unsupported.")
        };
        return authentication.Succeeded
            ? ExecutionEndpointRequestAuthenticationResult.Success(endpoint)
            : authentication;
    }

    private static async Task<ExecutionEndpointRequestAuthenticationResult> AuthenticateMutualTlsAsync(
        HttpContext context,
        ExecutionEndpointCredentialBinding credential,
        CancellationToken cancellationToken)
    {
        var certificate = await context.Connection.GetClientCertificateAsync(cancellationToken);
        if (certificate is null)
            return ExecutionEndpointRequestAuthenticationResult.Fail("A client certificate is required.");

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(credential.CredentialReference);
        }
        catch (FormatException)
        {
            return ExecutionEndpointRequestAuthenticationResult.Fail("The provisioned certificate fingerprint is invalid.");
        }

        var actual = certificate.GetCertHash(HashAlgorithmName.SHA256);
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected)
            ? ExecutionEndpointRequestAuthenticationResult.Success()
            : ExecutionEndpointRequestAuthenticationResult.Fail("Client certificate fingerprint does not match the endpoint.");
    }

    private async Task<ExecutionEndpointRequestAuthenticationResult> AuthenticateSignedRequestAsync(
        HttpContext context,
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        var timestampText = context.Request.Headers[TimestampHeader].ToString();
        var nonceText = context.Request.Headers[NonceHeader].ToString();
        var signatureText = context.Request.Headers[SignatureHeader].ToString();
        if (!long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out var unixTimestamp) ||
            !Guid.TryParse(nonceText, out var nonce) || nonce == Guid.Empty ||
            string.IsNullOrWhiteSpace(signatureText))
        {
            return ExecutionEndpointRequestAuthenticationResult.Fail("Signed execution request headers are invalid.");
        }

        DateTimeOffset requestTimestamp;
        try { requestTimestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp); }
        catch (ArgumentOutOfRangeException)
        {
            return ExecutionEndpointRequestAuthenticationResult.Fail("Signed execution request timestamp is invalid.");
        }

        var now = DateTimeOffset.UtcNow;
        var maxSkew = TimeSpan.FromSeconds(Math.Clamp(_options.SignedRequestMaxClockSkewSeconds, 30, 900));
        if ((now - requestTimestamp).Duration() > maxSkew)
            return ExecutionEndpointRequestAuthenticationResult.Fail("Signed execution request timestamp is outside the allowed clock skew.");

        if (context.Items[ExecutionRequestBodyHashMiddleware.BodySha256ItemKey] is not string bodySha256)
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution request body hash is unavailable.");

        byte[] signature;
        try { signature = Convert.FromBase64String(signatureText); }
        catch (FormatException)
        {
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution request signature is not valid Base64.");
        }

        var publicKeyPem = endpoint.CredentialBinding!.PublicKeyPem;
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution endpoint public key is not provisioned.");

        var canonical = BuildCanonicalRequest(context.Request, endpoint.Id, unixTimestamp, nonce, bodySha256);
        bool verified;
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);
            verified = ecdsa.VerifyData(
                Encoding.UTF8.GetBytes(canonical),
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException)
        {
            verified = false;
        }

        if (!verified)
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution request signature verification failed.");

        var retention = TimeSpan.FromSeconds(Math.Clamp(_options.NonceRetentionSeconds, 300, 86400));
        var nonceRecord = ExecutionEndpointRequestNonce.Create(endpoint.Id, nonce, requestTimestamp, now.Add(retention));
        if (!await _store.TryRegisterNonceAsync(nonceRecord, cancellationToken))
            return ExecutionEndpointRequestAuthenticationResult.Fail("Execution request nonce has already been used.");

        return ExecutionEndpointRequestAuthenticationResult.Success();
    }

    public static string BuildCanonicalRequest(
        HttpRequest request,
        Guid endpointId,
        long unixTimestamp,
        Guid nonce,
        string bodySha256)
    {
        return string.Join('\n',
            request.Method.ToUpperInvariant(),
            request.Path.Value ?? string.Empty,
            request.QueryString.Value ?? string.Empty,
            endpointId.ToString("D"),
            unixTimestamp.ToString(CultureInfo.InvariantCulture),
            nonce.ToString("D"),
            bodySha256.ToLowerInvariant());
    }
}
