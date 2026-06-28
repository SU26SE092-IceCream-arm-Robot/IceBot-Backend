using Domain.Common;

namespace Domain.Devices.Entities;

public class ExecutionEndpointRequestNonce : AppendOnlyEntity
{
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid Nonce { get; private set; }
    public DateTimeOffset RequestTimestamp { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ExecutionEndpointRequestNonce() { }

    public static ExecutionEndpointRequestNonce Create(
        Guid endpointId,
        Guid nonce,
        DateTimeOffset requestTimestamp,
        DateTimeOffset expiresAt)
    {
        if (endpointId == Guid.Empty || nonce == Guid.Empty || expiresAt <= requestTimestamp)
            throw new DomainRuleException("Endpoint, nonce, and a valid nonce expiry are required.");

        return new ExecutionEndpointRequestNonce
        {
            KioskExecutionEndpointId = endpointId,
            Nonce = nonce,
            RequestTimestamp = requestTimestamp,
            ExpiresAt = expiresAt
        };
    }
}
