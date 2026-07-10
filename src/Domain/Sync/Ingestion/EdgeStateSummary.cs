using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;

namespace Domain.Sync.Ingestion;

public class EdgeStateSummary : AuditedEntity
{
    public Guid KioskId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid SourceExecutorId { get; private set; }
    public string SummaryKind { get; private set; } = null!;
    public long StateRevision { get; private set; }
    public int SummarySchemaVersion { get; private set; }
    public DateTimeOffset EdgeCreatedAt { get; private set; }
    public DateTimeOffset CloudReceivedAt { get; private set; }
    public string PayloadJson { get; private set; } = null!;

    public virtual Kiosk Kiosk { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private EdgeStateSummary()
    {
    }

    public static EdgeStateSummary Create(
        Guid kioskId,
        Guid endpointId,
        Guid sourceExecutorId,
        string summaryKind,
        long stateRevision,
        int schemaVersion,
        DateTimeOffset edgeCreatedAt,
        DateTimeOffset cloudReceivedAt,
        string payloadJson)
    {
        Validate(summaryKind, stateRevision, schemaVersion, payloadJson);
        return new EdgeStateSummary
        {
            KioskId = kioskId,
            KioskExecutionEndpointId = endpointId,
            SourceExecutorId = sourceExecutorId,
            SummaryKind = summaryKind.Trim(),
            StateRevision = stateRevision,
            SummarySchemaVersion = schemaVersion,
            EdgeCreatedAt = edgeCreatedAt,
            CloudReceivedAt = cloudReceivedAt,
            PayloadJson = payloadJson
        };
    }

    public void ApplyNewerRevision(
        long stateRevision,
        int schemaVersion,
        DateTimeOffset edgeCreatedAt,
        DateTimeOffset cloudReceivedAt,
        string payloadJson)
    {
        Validate(SummaryKind, stateRevision, schemaVersion, payloadJson);
        if (stateRevision <= StateRevision)
        {
            throw new DomainRuleException("State summary revision must be newer than the stored revision.");
        }

        StateRevision = stateRevision;
        SummarySchemaVersion = schemaVersion;
        EdgeCreatedAt = edgeCreatedAt;
        CloudReceivedAt = cloudReceivedAt;
        PayloadJson = payloadJson;
    }

    private static void Validate(string summaryKind, long stateRevision, int schemaVersion, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(summaryKind) || stateRevision <= 0 || schemaVersion <= 0 ||
            string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new DomainRuleException("State summary kind, revision, schema version, and payload are required.");
        }
    }
}
