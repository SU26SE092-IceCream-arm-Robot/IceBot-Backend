using Domain.Common;

namespace Domain.Devices.Connectivity;

public sealed class KioskConnectivityProjection : GuidEntity
{
    public Guid KioskId { get; private set; }
    public KioskConnectivityStatus Status { get; private set; } = KioskConnectivityStatus.Unknown;
    public Guid? OriginNodeId { get; private set; }
    public long? LastHeartbeatSequence { get; private set; }
    public DateTimeOffset? LastObservedAt { get; private set; }
    public DateTimeOffset LastTransitionedAt { get; private set; }

    private KioskConnectivityProjection() { }

    public static KioskConnectivityProjection Create(Guid kioskId, DateTimeOffset createdAt) => new()
    {
        KioskId = kioskId,
        Status = KioskConnectivityStatus.Unknown,
        LastTransitionedAt = createdAt
    };

    public bool Observe(
        KioskConnectivityStatus status,
        Guid originNodeId,
        long heartbeatSequence,
        DateTimeOffset observedAt)
    {
        if (KioskId == Guid.Empty || originNodeId == Guid.Empty || heartbeatSequence <= 0)
            throw new DomainRuleException("Connectivity observation identity and sequence are required.");
        if (LastObservedAt.HasValue && observedAt < LastObservedAt.Value) return false;

        var changed = Status != status;
        Status = status;
        OriginNodeId = originNodeId;
        LastHeartbeatSequence = heartbeatSequence;
        LastObservedAt = observedAt;
        if (changed) LastTransitionedAt = observedAt;
        return changed;
    }

    public bool MarkUnreachable(DateTimeOffset observedAt)
    {
        if (Status == KioskConnectivityStatus.Unreachable) return false;
        Status = KioskConnectivityStatus.Unreachable;
        LastTransitionedAt = observedAt;
        return true;
    }
}
