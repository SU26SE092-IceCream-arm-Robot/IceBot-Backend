# Sequence Diagram — Robot Command Dispatch, Execution Evidence, and Timeout Recovery

**Document type**: Team-facing UML baseline (working draft), part of the `deliverables/03_uml/` set.

**Source basis**: `deliverables/00_repo_evidence/functional_inventory.md` (Sync section: SYNC-01–SYNC-03; MQTT Edge Integration section: MQTT-01–MQTT-03; IoT REST Contract section: IOT-05–IOT-09) and `deliverables/02_srs/srs.md` §4.13, §4.15 (FR-120–FR-127, FR-130–FR-132), cross-checked against `deliverables/00_repo_evidence/repo_truth_map.md` §8. No `src/` or `docs/` files were modified; `srs.md`/`project_introduction.md` were not modified.

**Scope note**: This diagram focuses specifically on the Cloud↔Edge **command/evidence contract** — how a command is dispatched, delivered, acknowledged, executed (Edge-internal, out of this repository's scope), reported back, and recovered from if something goes missing. It intentionally does not re-show the checkout/payment steps that trigger dispatch — see `sequence_order_flow.md` for those.

---

## Diagram

```mermaid
sequenceDiagram
    participant Job as Dispatch Reconciliation Job
    participant CloudDB as Cloud (EdgeCommand store)
    participant Broker as MQTT Broker
    participant Edge as Local Edge Backend
    participant Robot as Robot arm + devices

    Note over Job,CloudDB: Order is ReadyForFulfillment (see sequence_order_flow.md)
    Job->>CloudDB: Create EdgeCommand(ExecuteOrder), attempt 1
    CloudDB--)Broker: Publish wake-up (icebot/execution-endpoints/{id}/commands/available, QoS1)
    Broker--)Edge: Deliver wake-up (best-effort, not retained)

    alt Edge received wake-up
        Edge->>CloudDB: POST commands/pull (MaxCommands)
    else Edge missed wake-up (periodic poll instead)
        Edge->>CloudDB: POST commands/pull (periodic, independent of MQTT)
    end
    CloudDB-->>Edge: Pending command(s), artifact URL enriched, delivery attempt recorded

    Edge->>CloudDB: POST commands/{id}/ack (Received/Accepted/Rejected/ExecutorBusy/DeliveryFailed)
    alt Accepted
        CloudDB->>CloudDB: Project acceptance onto Order status
        Edge->>Robot: Run robot program (Edge-internal; robot control is out of this repo's scope)
        Robot-->>Edge: Physical execution result
    else Rejected / ExecutorBusy / DeliveryFailed
        CloudDB->>CloudDB: Project rejection onto Order status
        Note over CloudDB: Manual redispatch or production-remake may follow<br/>(FR-060/FR-061 — outside this diagram)
    end

    par Edge reports over REST (fallback)
        Edge->>CloudDB: POST commands/{id}/reports (SourceEventId, SequenceNumber, provenance checksum)
    and Edge reports over MQTT uplink (primary, same handler)
        Edge--)Broker: Publish to icebot/execution-endpoints/{id}/uplink/execution-report
        Broker--)CloudDB: Deliver to shared-subscription consumer group
    end

    CloudDB->>CloudDB: Validate provenance checksum against accepted command
    CloudDB->>CloudDB: Apply idempotently by (SourceEventId, SequenceNumber)
    CloudDB->>CloudDB: Update OrderExecutionRecord / ProductionExecutionRecord
    CloudDB--)Edge: Publish EdgeUplinkResult (MQTT path only)
    CloudDB--)Job: (via SignalR to UI) OrderExecutionObservationChanged / OrderStatusChanged

    Note over Job,CloudDB: Periodic timeout reconciliation (independent of the happy path above)
    Job->>CloudDB: Scan commands/executions past timeout threshold
    alt No ack or no report within threshold
        Job->>CloudDB: Mark observation Stale/Delayed, then Unreachable/PendingRecovery/SupportRequired
        CloudDB--)Job: OrderExecutionObservationChanged (never asserts a physical outcome)
    end
```

## Explanation

- **Two independent delivery paths, one durable record**: MQTT is only ever a best-effort *notification* that a command is waiting — it never carries the command payload, and Edge must always fall back to periodic REST polling regardless of whether the MQTT wake-up arrived (BR-04 in `srs.md` §7). The diagram shows this with an `alt` block rather than assuming MQTT delivery.
- **Command acknowledgement branches**: an `Accepted` ack lets robot execution proceed; any other ack outcome (`Rejected`/`ExecutorBusy`/`DeliveryFailed`) stops the happy path and instead feeds into the separate manual-redispatch/remake workflows (FR-060, FR-061), which are outside this diagram's scope by design (see `sequence_order_flow.md`/`activity_order_flow.md` for the wider order lifecycle).
- **Dual-transport execution reporting**: unlike command delivery, execution *evidence* (heartbeat, telemetry, readiness, execution reports, production-sync events) genuinely has two working paths — REST and MQTT both dispatch to the identical Application-layer handler, so either transport reaches the same durable Cloud record (`srs.md` BR-04, second half). The `par` block reflects that either path is valid, not that both always fire for the same event.
- **Reconciliation never asserts a physical outcome**: the periodic timeout job only changes an *observation* status (`Stale`, `Unreachable`, `PendingRecovery`, `SupportRequired`) — it does not guess whether the robot actually produced the item. This is called out explicitly because it is easy to misread "timeout reconciliation" as "automatic failure/success determination," which the evidence does not support.
- Robot arm control itself (`Edge->>Robot`) is drawn as a single opaque step because this backend does not control the robot arm directly — it only authors and distributes Lua programs ahead of time and receives execution evidence back; the Edge runtime that actually drives the robot is a separate system outside this repository's scope.

## Evidence Notes

- Automatic order execution dispatch (`EdgeCommand(ExecuteOrder)` creation, MQTT wake-up on commit): `functional_inventory.md` SYNC-01; `srs.md` FR-130.
- MQTT command-available wake-up (best-effort, QoS1, not retained, publish failure does not roll back the command): `functional_inventory.md` MQTT-01; `srs.md` FR-125.
- Edge command pull (REST, up to `MaxCommands`, artifact URL enrichment, delivery-attempt recording): `functional_inventory.md` IOT-05; `srs.md` FR-120.
- Edge command acknowledgement (state transitions, order-status projection, clock-skew window): `functional_inventory.md` IOT-06; `srs.md` FR-121.
- MQTT edge uplink consumption dispatching to the same Application handler as the REST endpoints, for all 6 message types (heartbeat/telemetry/readiness/execution-report/production-events/state-summaries): `functional_inventory.md` MQTT-02; `srs.md` FR-126.
- MQTT topic parsing, retained-message rejection, payload-size guard: `functional_inventory.md` MQTT-03; `srs.md` FR-127.
- Execution report ingestion (provenance-checksum validation, idempotent apply by `(SourceEventId, SequenceNumber)`, realtime publication on commit): `functional_inventory.md` IOT-07; `srs.md` FR-122. `[Inferred]` the "HTTPS is recovery fallback / MQTT is primary transport" framing is `srs.md`'s own wording (FR-122 Trigger field) inferred from the two co-existing endpoints; no separate transport-priority contract document was found — see `requirements_traceability_matrix.md`'s FR-122 row.
- Horizontally shareable MQTT consumption (`$share/{group}/...` topic groups): `functional_inventory.md` MQTT-02; `srs.md` NFR-016. `[Inferred]` broker-level shared delivery reduces duplicate *delivery*; it does not by itself prove "no duplicate processing," which additionally depends on the per-message idempotency shown above.
- Order execution timeout reconciliation (observation-status transitions only, never a physical-outcome assertion): `functional_inventory.md` SYNC-02; `srs.md` FR-130.
- Execution metrics collection (30s timer, stale/unreachable counts): `functional_inventory.md` SYNC-03; `srs.md` FR-131 (omitted from the diagram itself for readability — it is a passive metrics side-effect of the same state, not a distinct message flow).
- Manual redispatch / production remake as the human-driven alternative to automatic recovery: `functional_inventory.md` ORD-06, ORD-07; `srs.md` FR-060, FR-061 (referenced, not diagrammed here).
- Robot arm / Fairino runtime relationship (this backend authors/distributes programs, does not control the arm directly): `functional_inventory.md` Robot Configuration section, RC-02/RC-08; `srs.md` §3.3.
