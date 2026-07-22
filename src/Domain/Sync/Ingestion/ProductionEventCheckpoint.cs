using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;

namespace Domain.Sync.Ingestion;

public class ProductionEventCheckpoint : AuditedEntity
{
    public Guid KioskId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid SourceExecutorId { get; private set; }
    public long LastContiguousSequenceNumber { get; private set; }
    public Guid? LastContiguousEventId { get; private set; }

    public virtual Kiosk Kiosk { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ProductionEventCheckpoint()
    {
    }

    public static ProductionEventCheckpoint Create(
        Guid kioskId,
        Guid endpointId,
        Guid sourceExecutorId)
    {
        if (kioskId == Guid.Empty || endpointId == Guid.Empty || sourceExecutorId == Guid.Empty)
        {
            throw new DomainRuleException("Production event checkpoint identity is required.");
        }

        return new ProductionEventCheckpoint
        {
            KioskId = kioskId,
            KioskExecutionEndpointId = endpointId,
            SourceExecutorId = sourceExecutorId
        };
    }

    public void AdvanceTo(long sequenceNumber, Guid eventId)
    {
        if (sequenceNumber != LastContiguousSequenceNumber + 1 || eventId == Guid.Empty)
        {
            throw new DomainRuleException("Production event checkpoint can advance only by one contiguous event.");
        }

        LastContiguousSequenceNumber = sequenceNumber;
        LastContiguousEventId = eventId;
    }
}
