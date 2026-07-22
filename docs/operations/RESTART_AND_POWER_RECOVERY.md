# Restart And Power Recovery

## Search Keywords

`restart recovery`, `power loss`, `Cloud restart`, `Edge restart`, `controller restart`, `MQTT restart`, `database restart`, `restart policy`, `ManualOnly`, `physical output uncertainty`

## Purpose

This document defines recovery authority when Cloud, PostgreSQL, MQTT, Edge, a robot
controller, a tablet, or an entire store restarts. It does not claim that the current
Edge runtime implements controller recovery end to end.

## Production Restart Policy

Every newly published `RobotProgram` manifest snapshots `restartPolicy`. A configuration
release carries that immutable value, and each `ExecuteOrder` command repeats it for the
selected program.

V1 supports only:

```text
ManualOnly
```

Cloud rejects publication or command payloads that request another policy. Existing
schema-1 program manifests and schema-3 execute commands that do not contain the field
are interpreted as `ManualOnly`.

`NotRestartable`, `RestartFromBeginningIfNoPhysicalOutput`, and
`ResumeFromCheckpoint` are reserved typed values, not enabled behavior. In particular,
an accepted command must never be replayed automatically after a runtime restart.

## Runtime Interruption Report

When Edge or the controller restarts during an active production job, Edge reports the
affected job with:

```text
reportType: ProductionExecution
status: RequiresManualIntervention
sourceProductionJobId: <exact interrupted job>
errorCode: RuntimeRestarted | ControllerRestarted | PowerInterrupted
physicalOutputMayHaveOccurred: true | false | null
```

The report must retain the exact order item and production-unit range. `null` means that
physical output is unknown. A value of `false` is evidence, but it does not itself create
a new command. Any remake still uses the explicit remediation flow and its existing
eligibility checks.

Cloud preserves completed unit and inventory evidence. It does not convert an
interruption into a successful completion, automatically refund the order, resume Lua,
or restart the artifact list from the beginning.

## Recovery Matrix

| Failure | Durable authority | Recovery behavior |
| --- | --- | --- |
| Cloud process restart | PostgreSQL committed state | Hosted reconciliation jobs resume after startup. Uncommitted requests fail and callers must retry with the existing idempotency identity. |
| PostgreSQL restart | Last committed database state | In-flight transactions may fail. The application does not claim transparent transaction replay; background jobs retry on later cycles. |
| MQTT broker restart | PostgreSQL `EdgeCommand` state | MQTT is only a wake-up. Periodic authenticated command pull recovers missed notifications without changing `commandId`. |
| Edge restart before command acceptance | Cloud command state plus Edge durable inbox | Edge pulls again and deduplicates by `commandId`. |
| Edge restart after command acceptance | Edge durable production-job ledger plus Cloud report history | Do not redeliver or rerun the accepted command. Report every interrupted active job as `RequiresManualIntervention`. |
| Robot controller restart during Lua | Exact active production job and physical evidence | Default to `ManualOnly`; do not resume at an inferred Lua line or rerun from artifact 1. |
| Tablet/kiosk restart | Cloud order/payment state | Reload through normal read APIs and realtime subscriptions. UI restart does not change fulfillment state. |
| Whole-store power loss | Cloud committed state plus Edge durable local state | Stop new admission until readiness is fresh. Preserve paid and in-progress orders as operational uncertainty; resolve interrupted jobs manually. |
| Edge disk full or local database unhealthy | Last durable local job state plus Cloud report history | Publish `NotReady`, reject before acceptance, and stop admitting work. If failure occurs during an active job, report `RequiresManualIntervention/LocalPersistenceLost`; physical output defaults to unknown when evidence cannot be recovered. |

## Edge Persistence Requirement

Before accepting an `ExecuteOrder` command, Edge must durably retain at least:

- `commandId`, order and release provenance;
- production job identity and unit range;
- selected program, ordered artifacts, and checksums;
- current job lifecycle and the last known physical-output evidence;
- executor event counters needed to preserve idempotency after restart.

The same local transaction must persist the ACK outbox intent. Only after that commit
may Edge send `Accepted` with `localStatePersisted=true`. Failure to commit returns a
storage-specific `Rejected` acknowledgement and must not start the controller.

Readiness requires writable storage, sufficient free bytes, a healthy local database,
and event backlog within its configured maximum. Log retention must be bounded so logs
cannot consume the command/event reserve. The Edge implementation should reserve or
preallocate a minimal emergency journal for active-job identity and failure evidence;
when both local emergency persistence and Cloud delivery are unavailable, Cloud timeout
remains operational uncertainty rather than proof of success or failure.

If this state cannot be recovered, Edge reports uncertainty. It must not manufacture a
successful completion or assume that no product was produced.

## Excluded Checkpoint Resume Gate

`ResumeFromCheckpoint` must remain rejected until a controller integration can provide
a durable, attestable checkpoint containing the production job identity, program
manifest checksum, artifact/run order, checkpoint identity, controller execution
session, and physical-output evidence. `ProductionEventCheckpoint` is telemetry sync
state and is not a robot-motion checkpoint.

## Related Docs

- [Failure Flow Index](../flows/FAILURE_FLOW_INDEX.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Production Incident Resolution Flow](../flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md)
- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
