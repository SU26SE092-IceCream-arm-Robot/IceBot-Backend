# Final Evidence Review

Reviewed against the repository on 2026-07-30:

- `deliverables/00_repo_evidence/database_inventory.md`
- `deliverables/00_repo_evidence/functional_inventory.md`
- `deliverables/00_repo_evidence/repo_truth_map.md`

Repository evidence sampled and cross-checked:

- `src/Domain/**`
- `src/Application/**`
- `src/Infrastructure/Data/**`
- `src/Infrastructure/Migrations/**`
- `src/Infrastructure/**/Jobs/**` and hosted services
- `src/WebAPI/Controllers/**`
- `src/WebAPI/GraphQL/**`
- `src/WebAPI/SignalR/**`
- `ARCHITECTURE.md`
- relevant files under `docs/architecture`, `docs/api`, `docs/flows`, `docs/iot`, and `docs/data`

This review does not modify the reviewed evidence. Findings below distinguish a repository fact from a documentation interpretation.

## Executive Assessment

The evidence set is broad and useful, but it is not yet safe to use as an unqualified source for a final SRS. The database inventory is generally the strongest of the three, apart from a material count error and some interpretation presented as fact. The functional inventory has impressive breadth, but it overuses `Implemented` where the stated business rule was not directly verified. The truth map is a good navigation summary, but several sections summarize intended architecture and documented flows rather than proven runtime behavior.

Before final SRS/report writing:

1. Correct the database counts.
2. Separate `Code-verified`, `Documentation-verified`, `Inferred`, and `Unclear`.
3. Add exact API evidence for every public contract used as an SRS requirement.
4. Add omitted platform/background capabilities or explicitly exclude them from functional scope.
5. Resolve the open semantic questions listed at the end of this review.

## 1. Unsupported or Overstated Claims

| Severity | Evidence file / claim | Repository finding | Required treatment |
| --- | --- | --- | --- |
| High | `database_inventory.md:19`: `IceBotDbContext` exposes “~130 DbSet<T> properties.” | `src/Infrastructure/Data/IceBotDbContext.cs:109-214` contains **98** `public DbSet<T>` properties. The inventory itself later reports approximately 99 physical tables, including the `AccountStores` implicit join table. | Correct to 98 DbSets and distinguish DbSets, mapped entity types, and physical tables. |
| High | `repo_truth_map.md:69-75`: the full checkout/payment/execution sequence is written as repository truth. | The cited source is primarily a flow document. Individual parts have code, but the truth map says controller contents were not opened and the checkout flow was only partially read. The end-to-end sequence, transaction scope, dispatch idempotency, reconciliation behavior, and finalization therefore were not all proven in that evidence pass. | Mark the flow summary `[Documented]`; mark runtime claims `[Code-verified]` only after checking the relevant handlers/jobs and transaction boundaries. |
| High | Many functional rows state detailed authorization, uniqueness, state-transition, event-publication, and tenant-scoping rules while citing only controller lines. | A controller proves route shape, request binding, and declared policy. It does not prove handler authorization semantics, database uniqueness, state-machine behavior, transactionality, or emitted events. | Split route/API evidence from behavioral evidence. Do not classify the entire requirement `Implemented` from controller evidence alone. |
| Medium | `functional_inventory.md` notes several handlers were classified `Implemented` from controller wiring/doc description or “pattern consistency.” | Existence/wiring and similarity to sibling handlers do not establish handler behavior, validation, tenancy enforcement, persistence, or failure handling. Examples explicitly admitted in the notes include device handlers/queries and option delete handlers. | Mark these rows `[Unclear]` or `Implemented — route/wiring verified, behavior unverified` until directly read or tested. |
| Medium | `repo_truth_map.md:5-9`: system purpose includes “ice cream / beverage style kiosks,” tablet checkout, QR/bank transfer, robot fulfillment, and centralized management as one proven statement. | `ARCHITECTURE.md` supports the broad automated vending/robot system. Product type, UI device, and payment-mode wording may be product/document context rather than a stable code contract. | Mark product-specific wording `[Inferred]` or cite the owning product requirement. Keep the backend purpose technology-neutral if the SRS is backend-only. |
| Medium | `repo_truth_map.md:11-20`: “event-driven integration” is stated as an architecture style covering sync, runtime, callbacks, and operations. | The repository contains event ingestion, background jobs, MQTT, SignalR, and callback handlers, but not all interactions are event-driven; many are synchronous HTTP/CQRS operations and MQTT is notification-only. | Reword as “uses event-driven mechanisms for selected integration paths,” not as a universal system style. |
| Medium | `repo_truth_map.md:34-38`: edge authentication modes are assigned directly to “Full Edge” and “low-cost controller.” | The mapping is documented and code contains endpoint authentication modes, but deployment configuration may determine the mode. It is not evidence that every instance of a profile necessarily uses only that mechanism under all configurations. | Mark `[Documented contract]`; verify authenticator selection and configuration validation before making it a universal SRS rule. |
| Medium | `database_inventory.md:324-381`: JSON fields are grouped by “why they exist.” | Field presence and schema-version columns are proven; the purpose categories are architectural interpretation. Several unversioned metadata/payload fields do not enforce the described role in the type system or database. | Mark category assignments `[Inferred]` unless the owning docs or code explicitly state the purpose. |
| Medium | `database_inventory.md:416`: approximately 99 tables “created across migration history.” | There are 98 DbSets plus the implicit `AccountStores` join table, making 99 plausible, but migration call counts can include tables later changed/dropped and are not equivalent to current model tables. | State “99 current mapped tables if counting `AccountStores`,” verified against the model snapshot, rather than deriving current truth only from cumulative `CreateTable` calls. |
| Low | `repo_truth_map.md:63`: Application folders “mirror these contexts.” | `Dashboard` and `EdgeIntegration` are application capabilities/integration modules, not bounded contexts in the listed domain map; `ProductionExecution` has a domain namespace but is not listed in the quoted Application folder set. | Replace “mirror” with “mostly align, with application-specific modules such as Dashboard and EdgeIntegration.” |

## 2. Missing Major Backend Features

The functional inventory covers many business workflows but omits or underrepresents these material backend capabilities:

| Missing or underrepresented capability | Repository evidence | Why it matters to the final report |
| --- | --- | --- |
| Data retention and cleanup | `src/Infrastructure/Persistence/Jobs/DataRetentionJob.cs` and its hosted-service registration | Defines operational data lifecycle, deletion/retention behavior, storage growth, and compliance assumptions. |
| Robot artifact object-storage validation and orphan cleanup | `RobotArtifactObjectStorageStartupValidator`, `RobotArtifactOrphanCleanupJob` | Object storage is a significant external dependency with startup and compensation/cleanup behavior. This should appear in non-functional requirements and operational flows. |
| MQTT credential reconciliation | `src/Infrastructure/Devices/Credentials/Jobs/MqttCredentialReconciliationJob.cs` | Credential lifecycle is more than provision/rotate/delete API endpoints; reconciliation affects security and recoverability. |
| Identity/bootstrap seeding | `IdentityBootstrapHostedService` | Establishes initial roles/accounts/permissions and startup behavior. It may be deployment-only, but must be explicitly included or excluded. |
| Payment method bootstrap | `PaymentMethodCatalogHostedService` | The functional inventory intentionally excludes it as “plumbing,” but the final report should still record required reference-data initialization. |
| Payment session reconciliation | `PaymentSessionReconciliationJob` | A major failure-recovery path for payment state and order progression. It is referenced indirectly in the truth-map flow but lacks a clearly identifiable functional inventory row. |
| Order execution dispatch reconciliation | `OrderExecutionDispatchReconciliationJob` | Critical recovery behavior when payment succeeds but initial command dispatch is missed. It should have a dedicated functional requirement/evidence row rather than only narrative flow coverage. |
| Deployment failure notification | `DeploymentFailureNotificationJob` / feature | The inventory mentions it in research notes, but final functional coverage should state trigger, recipient, deduplication, retry, and delivery behavior. |
| Production package upgrade reconciliation | `ProductionPackageUpgradeReconciliationJob` / service | Long-running upgrade recovery is a major operational workflow and should be independently traceable. |
| Health/readiness/diagnostic semantics | `HealthEndpointExtensions.cs`, `/health`, `/health/ready`, `/management/diagnostics/health`, `/info` | The truth map lists only the surface. The final report needs dependency checks, authentication differences, and readiness versus liveness semantics. |
| API versioning, validation, exception/problem response behavior, rate limits, CORS, and request-size limits | WebAPI configuration/middleware | These are cross-cutting public-contract requirements and are not captured by a controller inventory. |
| Audit/soft-delete behavior as a functional/platform capability | entity bases, DbContext conventions, configurations | The database inventory describes structural patterns, but the final SRS needs actor attribution, visibility, restoration policy, and whether soft-deleted rows remain queryable. |

If these are intentionally outside functional scope, the final report should list them under platform operations/exclusions instead of silently omitting them.

## 3. Missing or Weak API Evidence

### 3.1 Truth-map API summary

`repo_truth_map.md` explicitly says controller contents were not opened. Its controller list therefore proves file existence only. It does not prove:

- exact route templates and HTTP verbs;
- API version behavior;
- authorization attributes/policies;
- anonymous versus authenticated access;
- request/response DTOs and status codes;
- headers such as idempotency keys and order access tokens;
- validation and error contracts;
- whether the route is registered and reachable;
- whether GraphQL is read-only in the current schema;
- SignalR hub authorization, groups, and client message contracts.

The final SRS should use controller/action source plus WebAPI endpoint registration as the minimum API evidence. OpenAPI output or integration tests should be preferred for the externally observable contract.

### 3.2 Functional inventory rows

For each REST row, evidence should separately identify:

1. Controller class route.
2. Action verb/template.
3. Authorization attribute/policy.
4. Request DTO and validators.
5. Handler/service.
6. Response DTO and explicit status codes.
7. Integration test or OpenAPI evidence, if present.

The current inventory frequently gives a controller range and a handler filename but not DTO, validator, response, or test evidence. This is especially risky where requirement prose adds rules not visible at the action boundary.

### 3.3 GraphQL

The repository contains GraphQL query types for Dashboard, Devices, Inventory, Orders, and Tenants. The evidence should record exact schema field names, arguments, result types, authorization, paging behavior, and nullability. “GraphQL-only” is useful but insufficient API evidence.

`repo_truth_map.md` should not state the phase is read-only without directly verifying `GraphQLEndpointExtensions` registrations and all root types. Current files found are query-oriented, so read-only is likely, but the evidence file itself admits it did not verify this.

### 3.4 SignalR

Hub route registration is proven in `src/WebAPI/SignalR/SignalREndpointExtensions.cs`:

- `/hubs/orders`
- `/hubs/operations`
- `/hubs/management-dashboard`

The final API evidence still needs hub authorization, subscription methods, group naming, payload schemas, delivery guarantees, reconnect expectations, and whether messages are deltas or authoritative state.

### 3.5 Health and information endpoints

The truth map groups `/health...` and `/info` as public probes. The repository also contains `/management/diagnostics/health`. These must be separated because they have different audiences and potentially different disclosure/authentication rules. Exact response fields and dependency checks should be evidenced.

## 4. Database and Entity Mismatches

| Finding | Evidence | Impact |
| --- | --- | --- |
| DbSet count is wrong | 98 `DbSet<T>` properties in `IceBotDbContext`, not ~130. | Material factual error in the headline database inventory. |
| DbSets, entity types, and tables are conflated | `AccountStores` has no domain class/DbSet but is a physical join table. Owned/implicit types and mapped projections also require clear counting rules. | Counts will disagree unless the inventory defines each unit. |
| Migration-derived total is not necessarily current schema truth | The inventory sums `CreateTable` calls. | Use `IceBotDbContextModelSnapshot` or generated model metadata for current schema; use migrations for evolution history. |
| `SyncEventInbox` naming is inconsistent by design | Property and table are singular, unlike most collection/table names. | Record as a naming irregularity; avoid “fixing” the name in SRS mappings. |
| Context ownership and physical relationship are mixed | Cross-context FKs exist, while architecture says contexts own their models. | The final data model should distinguish ownership from database referential links. |
| Scope semantics are non-uniform | Some records carry full nullable scope fields and `ScopeType`; others have required organization scope; others derive scope through parents. | Do not describe a single uniform tenant-column strategy. Model the variants explicitly. |
| `RobotProgram` exposes `Global` in the shared enum but rejects it | Noted by `database_inventory.md`; verified as a semantic mismatch between enum vocabulary and aggregate rules. | Final SRS must say which scopes are actually allowed per entity, not repeat the global hierarchy mechanically. |
| `ProductOption.TemplateProductOptionId` lacks a configured FK | Noted by the inventory. | Clarify whether it is an intentionally unenforced lineage identifier or incomplete persistence mapping. |
| Parallel history entities use different base types | `OrderStatusHistory` has business audit/soft-delete fields; `OrderItemStatusHistory` and `ProductionIncidentHistory` use a bare GUID base. | Avoid claiming a uniform append-only/audit policy. Decide intended retention and correction semantics. |
| JSON schema-version practice is inconsistent | Some JSON fields have explicit schema versions; metadata/raw evidence fields often do not. | Final SRS should enumerate which JSON contracts are versioned and which are opaque. |
| Tenant uniqueness often uses nullable composite indexes | PostgreSQL unique-index behavior with nullable columns may permit multiple rows that business prose calls unique unless additional guards exist. | Verify each “unique within scope” requirement in handler/domain logic and actual index filters. |

## 5. Terminology Conflicts

| Terms | Conflict / ambiguity | Recommended SRS vocabulary |
| --- | --- | --- |
| Sales Catalog / menu / runtime menu / catalog | Catalog owns product definitions; Sales Catalog owns sellable menu offers; Edge holds a runtime projection. Evidence sometimes compresses these into “catalog.” | Define all three and use “runtime menu projection” only for Edge-consumable/read model data. |
| Robot Configuration / Production Configuration / Production Execution / Edge execution | Robot artifacts/programs are configuration; releases/routes bind executable configuration; Cloud execution records are projections; Edge owns actual runtime execution. | Keep these four responsibilities distinct. Never call Cloud projection records robot jobs. |
| Kiosk status / operational state / connectivity / readiness / activity / safety | These are independent state dimensions but are sometimes summarized as “kiosk status.” | Use the exact dimension name in every requirement. Reserve “status overview” for an aggregate read model. |
| Device / execution endpoint / Full Edge runtime / controller / robot target | These identifiers are related but not interchangeable. | Define execution endpoint as the authenticated Edge/Cloud integration boundary; define attached runtime/controller/robot target separately. |
| Organization scope / tenant scope / role scope | “Tenant” can mean organization root, the full organization-store-kiosk hierarchy, or an authorization assignment. | Define tenant root and resource scope separately from RBAC role assignment scope. |
| Payment status / transaction status / settlement disposition / refund status | Order payment summary is not the same state machine as a payment transaction or refund. | Give each state owner a separate glossary entry and transition table. |
| Sync / production sync / execution report ingestion / MQTT uplink | These share infrastructure but have different event families and replay support. | Name the channel and event family. Do not imply all dead letters support replay. |
| Implemented / documented-only / partial | `Implemented` currently ranges from direct handler inspection to route wiring or pattern inference. | Use evidence dimensions: `Route verified`, `Behavior verified`, `Persistence verified`, `Test verified`, `Documented`, `Inferred`, `Unclear`. |
| Actor names versus authorization roles | “Tablet,” “Edge runtime,” “payment provider,” and “system job” are actors but not RBAC roles. | Separate human roles, machine principals, external systems, and scheduled processes. |

## 6. Claims That Should Be Marked `[Inferred]` or `[Unclear]`

### Mark `[Inferred]`

- Product-domain description “ice cream / beverage style kiosks” unless backed by an owning product requirement.
- JSON-field purpose groupings where purpose is derived from field names rather than an explicit contract.
- “Application folders mirror bounded contexts”; the alignment is approximate.
- `AccountStores` business meaning beyond its EF many-to-many mapping.
- Any assertion that a controller/handler naming pattern guarantees sibling behavior.
- “At most one” or uniqueness claims that depend on nullable PostgreSQL composite indexes without explicit null-handling analysis.
- Responsibilities attributed to an actor solely from route placement.
- Claims that a background job guarantees recovery rather than periodically attempting reconciliation.

### Mark `[Unclear]` until directly verified

- Exact request/response shapes and status codes for endpoints not read line by line.
- Full authorization/tenant-scope behavior for rows evidenced only by policies or controller wiring.
- GraphQL being completely read-only.
- End-to-end transaction boundaries across payment callback, order transition, and edge-command creation.
- Delivery guarantees for SignalR and MQTT notifications.
- Whether every documented flow stage has active DI registration and production configuration.
- The temporary-password account path: code exists, while the cited onboarding rules say it is not part of the current contract. This is a contract conflict, not merely a partial feature.
- Delete semantics and business guards for handlers inferred from sibling patterns.
- Current schema table total until checked against the model snapshot with an explicit counting rule.
- Whether manually executed migration-step classes are part of the normal deployment process.
- Whether data retention, orphan cleanup, reconciliation, and notification jobs are enabled in every deployment profile.

## 7. Important Open Questions for Final SRS / Report Writing

### Scope and system boundary

1. Is the SRS describing the Cloud backend only, the Edge runtime too, or the end-to-end IceBot system?
2. Are tablet, Edge runtime, controller firmware, MQTT broker, object storage, email/push providers, and PayOS external systems or components within delivery scope?
3. Are operational/bootstrap jobs functional requirements, deployment requirements, or explicit exclusions?

### API contracts

4. Is generated OpenAPI the authoritative REST contract, or are controllers/DTOs authoritative?
5. What are the stable public response/error envelopes, status codes, paging conventions, and validation format?
6. Which endpoints are anonymous, order-token authenticated, JWT authenticated, mTLS authenticated, or ECDSA-signed?
7. What idempotency key is required for each retryable command, what is its scope, and how long is it retained?
8. Is GraphQL guaranteed read-only for the target release? Which fields and authorization rules are contractual?
9. What SignalR messages, groups, payload versions, and reconnect/resync behavior are contractual?
10. Are health and `/info` responses intentionally public, and what information may they disclose?

### Data and tenancy

11. What is the authoritative current table/entity count and counting convention?
12. Which tenant-scoped entities permit Global, Organization, Store, Kiosk, or Device scope in practice?
13. What prevents cross-tenant references where the database FK validates identity but not tenant ownership?
14. Is `ProductOption.TemplateProductOptionId` intentionally a non-FK lineage field?
15. Are history records immutable, soft-deletable, correctable, or permanently append-only?
16. Which JSON fields have public/stable schemas, and what are their supported schema versions?
17. What is the retention policy for callbacks, device events, heartbeats, sync inbox rows, dead letters, nonces, operation logs, and execution evidence?
18. Are migration manual-step classes mandatory deployment steps, and how are they invoked/audited?

### Workflow semantics

19. What is the exact atomic boundary when payment is confirmed: transaction update, order update, history, and edge-command creation?
20. What happens when payment succeeds after the order payment deadline or after cancellation?
21. What are the recovery guarantees when command dispatch, MQTT wake-up, Edge acknowledgement, execution report, or final state ingestion is missed?
22. Which event families support replay from dead letter, and what is the required operator process for unsupported families?
23. Who owns final truth when Cloud state, Edge state summary, and execution report disagree?
24. What are the compensation rules for failed object-storage writes, database commits, artifact cleanup, deployment, and package upgrade?
25. Which background reconciliation jobs are required for correctness versus observability only?

### Payments, refunds, and incidents

26. Is PayOS the only provider in scope or merely the current adapter?
27. Is “manual cash refund only” a confirmed release requirement? How is completion evidenced and audited?
28. How are duplicate or late provider callbacks deduplicated and reconciled?
29. How do `Order.PaymentStatus`, `PaymentTransaction.Status`, settlement disposition, refund status, and incident resolution interact?
30. Is voucher issuance implemented, planned, or only documented as an option?

### Security and operations

31. What are credential issuance, rotation, revocation, expiry, and recovery requirements for JWT, refresh tokens, endpoint credentials, MQTT credentials, and mTLS certificates?
32. Which secrets or raw payloads may be persisted, logged, or returned through diagnostics?
33. What are rate limits and request-size limits for public checkout, webhook, telemetry batch, production sync, and artifact upload endpoints?
34. What startup/bootstrap behavior is permitted in production, and is it idempotent?
35. What availability, recovery-time, monitoring, alerting, and backup/restore requirements apply?

## Final Recommendation

Use the three files as discovery material, not yet as final traceability evidence. For the final SRS, create a requirement-to-evidence matrix with separate columns for:

- owning bounded context;
- actor/principal;
- public contract;
- code evidence;
- persistence evidence;
- authorization/tenancy evidence;
- failure/retry/idempotency evidence;
- automated test evidence;
- confidence (`Verified`, `Documented`, `Inferred`, `Unclear`);
- open decision.

The highest-priority corrections are the 98-versus-130 DbSet error, the overstatement of end-to-end documented flows as proven runtime behavior, and `Implemented` classifications based only on wiring or pattern similarity.
