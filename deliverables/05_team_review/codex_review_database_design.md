# Codex Review — Database Design Deliverables

## Review Scope

Reviewed without modifying:

- `deliverables/04_database_design/conceptual_database_design.md`
- `deliverables/04_database_design/logical_database_design.md`
- `deliverables/04_database_design/physical_database_design.md`
- `deliverables/00_repo_evidence/database_inventory.md`
- `deliverables/03_uml/erd.md`
- `deliverables/03_uml/class_diagram.md`
- `deliverables/02_srs/srs.md`
- `deliverables/02_srs/requirements_traceability_matrix.md`

This document contains review comments only.

## Executive Assessment

The three database-design documents have a sensible intended separation and the physical table-name list is broadly complete. However, there are several factual errors and many cases where simplified UML or conceptual language has been promoted into unsupported cardinality or physical-schema claims.

Highest-priority findings:

1. The physical design conflates **98 DbSets/entity types** with physical tables. The listed model includes the `AccountStores` join table, so the current physical count cannot simultaneously be “98 tables/entities.”
2. `logical_database_design.md` gives `DeviceType` a GUID identifier, conflicting with the evidence that `DeviceType` is a `CatalogEntity` with a database-generated long key.
3. `ExecutionEndpointCapabilityProjection` is incorrectly collapsed into a 1:1 endpoint projection. It is a child collection under `ExecutionEndpointReadinessProjection`, unique by readiness projection plus capability code.
4. `OrderItem` → `ProductionIncident` is incorrectly modeled as 0..1. The database evidence includes production-unit-level incident indexing and does not support a one-incident-per-item constraint.
5. The conceptual document claims conceptual entities map one-to-one to physical entities, then introduces merged or synthetic concepts that map to multiple/no single table.
6. Several physical-index claims are copied from the inventory without distinguishing “non-deleted” from domain lifecycle `Active`, and the unresolved global `Restrict` versus explicit `Cascade` behavior prevents definitive delete semantics.

## 1. Unsupported Entities, Attributes, Tables, or Relationships

### Conceptual design

| Claim | Review comment |
| --- | --- |
| “Each conceptual entity maps one-to-one to a physical entity.” | Contradicted within the document. `Inventory Topology Event` merges two tables; `Kiosk Deployment` merges Full-Edge and controller deployment structures; `Execution Evidence Record` combines two projection tables; `Sync Event / Dead Letter` combines multiple entities; `Dispenser` renames `IngredientDispenserState`. Mark as a many-to-one conceptual abstraction, not one-to-one mapping. |
| Organization “employs” User Account via role assignment | Employment is not represented. `AccountRole` grants scoped authorization and can be platform-wide or scoped at multiple levels. Use “authorizes/assigns access” and mark employment `[Inferred]`. |
| Every Kiosk is “running a robot arm” | A kiosk can have devices/endpoints/targets, but the database inventory does not prove every Kiosk always has an installed robot arm. Mark `[Assumption]`. |
| Menu relationship drawn as `KIOSK ||--o{ MENU` | `Menu.KioskId` is nullable and Menu can be Organization- or Store-scoped. A direct mandatory Kiosk parent is false for non-kiosk-scoped menus. |
| Product Variant has “only one active/default recipe at a time” | The physical predicate enforces at most one **default non-retired** recipe per variant. It does not say at most one Active recipe unless domain code separately enforces that. Preserve the exact predicate. |
| Order Line Item may raise at most one Production Incident | Unsupported and likely false. The physical model indexes incidents by `(OrderId, OrderItemId, ProductionUnitNo)` and supports incident history across production units. No unique `OrderItemId` constraint is cited. |
| Refund is “money or voucher” | `Refund` stores amount/status/provider-refund fields. The evidence says the current phase is manual cash refund only and does not establish a voucher entity/type in `Refund`. Mark voucher `[Unclear]` or remove from the data model. |
| Robot Artifact is globally/shared or organization-owned | `RobotArtifact` requires `OrganizationId`; the global/shared counterpart is `RobotArtifactTemplate`. `RobotArtifactTechnicalContract` may be global or organization-scoped. These must not be merged. |
| Production Package Installation produces one Configuration Release | The inventory describes materializations/compositions and package workflows, but the exact one-installation-to-one-release cardinality is not established in the cited entity summary. Mark `[Inferred]`. |
| Edge Command “once acted on” has Execution Evidence Records | Execution projections are accepted evidence/read models and may be absent, delayed, or multiple. “Once acted on” implies a guaranteed relationship not established by FK/cardinality evidence. |
| Payment Method is “currently PayOS” | `PaymentMethod` is a catalog entity with Provider/MethodType/IsOnline. PayOS is the current adapter/bootstrap row, not the conceptual meaning or exclusive cardinality of Payment Method. |

### Logical design

| Claim | Review comment |
| --- | --- |
| `DeviceType` identifier is GUID | Conflicts with `database_inventory.md` §7, which lists `DeviceTypes` among `CatalogEntity`-derived long-key tables. It should be a database-generated long identifier. |
| `ExecutionEndpointReadinessProjection`, `ExecutionEndpointCapabilityProjection` share endpoint FK, 1:1, and StateRevision | Incorrect aggregation. The true endpoint 1:1 is readiness projection. Capability rows are a collection below the readiness projection, uniquely keyed by `(ExecutionEndpointReadinessProjectionId, CapabilityCode)`. |
| `OrderItem` (0..1) — (0..1) `ProductionIncident` | Unsupported. The dependent has required `OrderItemId`, but multiple incidents may exist for separate production units. Model `OrderItem` 1 → 0..N incidents unless a unique constraint proves otherwise. |
| `SyncEventInbox` 0..1 ↔ 0..1 `SyncDeadLetter` | The inventory does not list this as a true 1:1 relationship. Nullable IDs and workflow semantics do not prove a unique FK. Mark `[Unclear]` until the configuration/index is cited. |
| `ProductionPackageInstallation` 1 → 0..1 `ConfigurationRelease` | No direct relationship is identified in the inventory’s notable FK list. “Via materialization” is an inferred workflow relationship, not necessarily an FK/cardinality. |
| `RobotProgramArtifact`, `ExecutionRouteRobotBinding`, and other joins use identifier “(join)” | These are mapped entity types/DbSets, not necessarily keyless join tables. The document must state their actual surrogate/composite identifier only when evidenced. `AccountStores` is the explicitly documented composite-PK join. |
| `RobotProgramArtifact.RunOrder` is unique per program | Plausible and likely configured, but the cited inventory summary only states `RunOrder > 0`; exact uniqueness must cite its index/constraint. Mark `[Unclear]` if not in the evidence. |
| `RobotArtifact.ArtifactCode` is an alternate key with checksum | The physical unique index is `(OrganizationId, ArtifactCode, Checksum)` among non-deleted rows. `ArtifactCode` alone is not proven unique. Wording should not imply it is an alternate key by itself. |
| Role assignments may be scoped to “one, two, or three tenant levels” | Nullable scope columns prove possible stored combinations, not the legal semantic combinations. Scope validation rules must be cited; mark this interpretation `[Inferred]`. |
| `Order` must have 1..N items | The document correctly raises an Open Question, but the relationship is still printed as 1..N and tagged `[Supported]`. Database FK direction only proves every OrderItem has an Order, not that every Order has an item. Use 0..N physically; application rule may require at least one at creation. |
| Parent relationships labeled “mandatory” while drawn 0..N | “Mandatory” applies to the dependent FK, not to existence of a child. For example a Kiosk need not have a Device, and an Organization need not have a Store. State “dependent-to-parent required; parent may have zero children.” |

### Physical design

| Claim | Review comment |
| --- | --- |
| “Full physical schema comprises 98 tables/entities” | 98 is the direct DbSet count. `AccountStores` is a physical join table without a DbSet/domain class. The listed physical table count is therefore at least 99 under that counting rule. Do not combine table and entity counts. |
| Migration sum of approximately 99 is a “lower bound” | Cumulative `CreateTable` calls are an evolution count, not inherently a lower bound on current tables; later drops/renames could make it higher than current state. Call it a cumulative creation count and verify current model snapshot separately. |
| All explicit parent-owned Cascade pairs | The document correctly flags uncertainty, but elsewhere ERD/design explanations describe the exceptions as though effective. Until model snapshot/migration FK actions are checked, only the intended per-configuration Cascade is supported. |
| JSON role assignments are `[Supported]` physical mapping | `*Json` → `jsonb` is physical and supported. The four roles and “why they exist” are logical/architectural interpretation and should be `[Inferred]` per the prior evidence review. |

## 2. Conceptual, Logical, and Physical Levels Are Mixed

### Conceptual document contains logical/physical detail

- Lifecycle enum values such as Draft → Published → Active → Retired.
- Authentication mechanisms (certificate versus ECDSA signed request).
- Immutable snapshots, checksums, schema behavior, and provider-specific PayOS/Fairino terms.
- “At most one” cardinality and exact optionality claims.
- Statements about order access tokens and stored/non-stored implementation.
- A note that each concept maps one-to-one to a physical entity.

These may be useful annotations, but they should be visually separated as implementation notes. The conceptual core should focus on business concepts, ownership, and high-level relationships without claiming FK-level multiplicity.

### Logical document contains physical and application concerns

- GUID versus database-generated long strategy is partly physical implementation, though logical identifiers can mention surrogate nature.
- “ActiveRowFilter,” soft-delete query filters, `WhereNotDeleted()`, and exact EF behavior belong to physical design.
- Application authorization hierarchy, cancellation permissions, activation preflight, and workflow transition guards are business/application rules, not relational constraints unless tied to stored state/invariants.
- Append-only claims are used as logical constraints even where the database has no immutability mechanism.

The logical design should distinguish:

- relational invariant enforced by schema;
- aggregate/application invariant enforced by code;
- workflow policy from SRS;
- inferred normalization rationale.

### Physical document contains logical/operational narrative

- Four-role JSON taxonomy belongs primarily to logical/data-contract design; only `jsonb`, columns, lengths, and schema-version columns are physical.
- “Why external storage keeps the DB lean,” retention business intent, and provider exclusivity are architecture/operations rationale.
- Advisory locks and jobs are operational persistence concerns, but should not be presented as table design.

The physical document can retain an operational appendix, but it should not blur current schema facts with deployment defaults and architectural rationale.

## 3. Missing Important Entities from `database_inventory.md`

### Conceptual design omissions or over-collapses

Conceptual design need not list all 98 mapped entities, but these concepts are important enough that omission/merging loses business meaning:

- `KioskOperationalStateTransition` — operational-state audit is distinct from Kiosk itself.
- `AccountNotificationDevice`, `RefreshToken`, `PasswordResetRequest` — identity/session/notification lifecycle concepts.
- `OrderItemOption`, `OrderItemOptionIngredientRequirement`, `OrderItemStatusHistory`, `ProductionIncidentHistory` — customization snapshots and fulfillment/incident evidence.
- `KioskHeartbeat`, `KioskConnectivityProjection`, readiness and capability projections — connectivity/readiness are separate Kiosk state dimensions emphasized in the SRS.
- `ExecutionEndpointCredentialBinding`, MQTT credential, request nonce, supported robot target — security and endpoint capability are materially different concepts.
- `ControllerArtifactSetDeployment` and items — low-cost deployment path should not be hidden entirely under generic Kiosk Deployment.
- Production Package materialization/composition/upgrade and rollback evidence — major workflows in the SRS.
- `NotificationDelivery` is listed, but its relationship to account installation/device delivery is underdeveloped.
- `ProductionEventCheckpoint`, `EdgeStateSummary`, `SyncDeadLetterRetryAttempt`, and `EdgeCommandDeliveryAttempt` — checkpoint/upsert, dead-letter recovery, and command delivery evidence have different semantics.

### Logical design omissions

- `KioskOperationalStateTransition` is absent from the Tenants entity table despite being in the physical inventory.
- `OrderItemOptionIngredientRequirement` is absent, although the physical model and database inventory list it.
- `KioskConnectivityProjection` is absent from Devices.
- `ExecutionEndpointCredentialBinding` is absent.
- Numerous Production Package child/upgrade entities are compressed into ellipses; acceptable for a summary, but not for a claimed logical database design.
- `ControllerArtifactSetItem` is merged into its deployment row.
- Full attributes/identifiers for many mapped entities are explicitly not itemized. Therefore the logical document is a selected logical summary, not a complete logical schema.

### Physical design

The physical-name inventory appears to contain the tables listed by `database_inventory.md`, including `AccountStores`. The issue is counting/classification, not an obvious missing table name. However, §4 details only a subset of table constraints, so it is not a complete physical design specification.

## 4. Physical Constraints and Indexes That Do Not Match or Need Qualification

### Table/entity count mismatch

- 98 = `DbSet<T>` properties.
- `AccountStores` = additional physical join table without DbSet/domain class.
- Approximately 99 = cumulative migration `CreateTable` calls according to the inventory.
- Current physical table count was not independently verified from `IceBotDbContextModelSnapshot` or a live schema.

The physical document must not call all three measures equivalent.

### Partial unique predicates

The six predicates in physical §4.3 match the inventory as written. Review cautions:

- “Active” is shorthand only. Each predicate has different status/boolean semantics.
- `Recipes` enforces default + non-retired, not necessarily one Active recipe.
- `KioskConfigurationDeployments` covers only the stated numeric statuses; verify enum-number stability if numeric predicates are documented externally.
- Payment primary uniqueness is unfiltered by soft delete according to the shown predicate; do not infer reusable primary settlement after deletion.

### Soft-delete-aware indexes

- “Active rows” should be replaced with “non-soft-deleted rows.” Domain lifecycle Active is a different concept.
- An index filtered by `DeletedAt IS NULL` allows key reuse after soft deletion at the database level, even for principal types excluded from the automatic query filter. Query-filter exceptions do not change the index predicate.
- The evidence list should preserve exact index columns. For example `RobotArtifact` uniqueness is `(OrganizationId, ArtifactCode, Checksum)`, not ArtifactCode alone.
- `FranchiseOnboarding.RequestChecksum` and idempotency index wording is ambiguous. Document the exact index column set and filter separately rather than writing `RequestChecksum/IdempotencyKey` as if one rule.

### Unfiltered uniqueness

“Unique even across soft-deleted rows” is accurate for retained soft-deleted rows, but “forever” would be too strong because physical deletion/migration can remove the row or constraint. The design mostly avoids “forever”; retain that precision.

### Delete behavior

The actual effective delete actions remain `[Unclear]` because the global loop may override per-configuration Cascade settings. This affects:

- technical contract children;
- robot authoring import items;
- production package parent/children;
- controller artifact set items.

Do not use the intended Cascade exceptions in ERD explanatory text as established runtime behavior until checked against the model snapshot/generated migration FK definitions.

### Missing physical details

- Primary key definitions and all FK names/actions are not enumerated.
- Column nullability/types/length overrides are not specified table by table.
- All unique indexes and ordinary indexes are not exhaustively listed.
- The actual current model snapshot is not used as a reconciliation source.
- `PublicKeyPem` length concern is noted but not resolved.
- No physical mapping for enum storage/numeric values is documented, despite filtered indexes using numeric status values.

## 5. Unsupported Cardinality or Optionality Claims

| Relationship | Issue |
| --- | --- |
| Kiosk → Menu | Menu can be Organization/Store/Kiosk scoped; Kiosk is optional. |
| Organization → User Account | No employment FK; relationship is indirect and potentially platform-wide through AccountRole. |
| OrderItem → ProductionIncident | Should not be 0..1 based on current evidence; no unique OrderItem FK is cited. |
| Order → OrderItem minimum 1 | Application creation rule may require items; database relationship alone permits an Order with zero persisted items. |
| Kiosk → Device | Device.KioskId is nullable, so Device → Kiosk is 0..1, not mandatory. |
| Endpoint → CapabilityProjection | Capabilities are 0..N children of readiness projection, not endpoint 1:1. |
| SyncEventInbox ↔ SyncDeadLetter | True 1:1 is not listed in the inventory; needs index/FK evidence. |
| Installation → ConfigurationRelease | “Via materialization” is a workflow inference, not proven direct 0..1 FK. |
| RobotProgram → RobotProgramArtifact minimum 1 | “Once published” is an application lifecycle rule, not the base relational cardinality. Physically it can be zero before publication. |
| TechnicalContract → child “mandatory” | A contract may have zero effects/constraints unless publish validation or DB constraint proves otherwise. Required child-to-parent FK does not require a parent to have children. |
| AccountRole scope combinations | Nullable columns do not establish all legal combinations. |
| PaymentTransaction → Refund | 0..N is structurally plausible; conceptual singular “a Refund” should not imply 0..1. |
| Kiosk → ExecutionEndpoint | KioskId is required on endpoint, but a Kiosk can have zero endpoints. Use required dependent parent, optional child collection. |

The UML deliverables explicitly trim and simplify relationships. They are useful cross-checks but cannot serve as authoritative proof of exact cardinality where database inventory §3 does not identify the relationship or unique FK.

## 6. Soft-Delete and Unique-Index Misstatements

1. Use **non-deleted**, not **active**, for `DeletedAt IS NULL` index filters.
2. Do not treat automatic query filtering and filtered uniqueness as the same mechanism. The 12 principal exceptions still participate in any explicitly filtered unique indexes.
3. Do not infer that all soft-deletable entities receive the query filter; the exception list is material.
4. Do not infer that all queries against the 12 exceptions use `WhereNotDeleted()`; the design correctly labels that `[Inferred]`, and this should remain an Open Question for query coverage.
5. Do not call append-only entities immutable at the database level unless update/delete prevention is evidenced. Base-class naming and application usage do not prove DB enforcement.
6. Do not convert composite uniqueness into single-field alternate keys:
   - RobotArtifact uses Organization + ArtifactCode + Checksum.
   - Product/Recipe/Menu/RobotProgram codes include scope fields.
   - Store/Kiosk codes include their documented tenant key.
7. Preserve partial-index predicates exactly; “one active” is not a shared generic rule.
8. The unresolved Restrict/Cascade issue means delete outcomes cannot yet be stated categorically for intended owned children.

## 7. Claims to Mark `[Inferred]`, `[Assumption]`, or `[Unclear]`

### Mark `[Inferred]`

- Every Kiosk physically runs a robot arm.
- Organization employs accounts.
- Production Package Installation produces exactly one Configuration Release.
- Conceptual execution evidence exists after every acted-on command.
- JSON role/purpose assignments beyond the supported `jsonb` mapping.
- Normal-form conclusions and normalization benefits.
- Every query against soft-delete-exception principals calls `WhereNotDeleted()`.
- Legal combinations of nullable AccountRole scope columns.
- Transport/provider-specific concepts treated as stable business entities.
- Any cardinality copied only from the trimmed ERD/class diagram rather than a unique FK/configuration.

### Mark `[Assumption]`

- Customer remains permanently anonymous with no future persistence.
- PayOS is the only future payment provider.
- PostgreSQL 17, `IceBotDB`, MinIO, and current configuration names are binding product requirements rather than deployment defaults.
- One conceptual business domain maps directly to one bounded context.
- Cumulative migration table count equals current deployed schema count.

### Mark `[Unclear]`

- Effective Cascade versus Restrict actions.
- Current physical table count under a defined counting rule.
- `ProductOption.TemplateProductOptionId` intended FK semantics.
- Missing `RobotProgram.TemplateProgramId` versus stale documentation.
- `PublicKeyPem` length sufficiency.
- JSON schema-version strategy for unpaired fields.
- Runtime/design-time connection-key divergence intent.
- Missing `EdgeCommandDeliveryAttempts.SentAt` index intent.
- Whether checkpoint/state-summary tables belong in high-volume planning.
- Exact SyncEventInbox–DeadLetter cardinality.
- Exact Installation–ConfigurationRelease relationship.
- Whether voucher compensation has a stored model outside `Refund`.

## 8. Gaps to List as Open Questions

1. What is the authoritative current-schema count: DbSets, mapped entity types, or physical tables, and does the model snapshot contain 99 tables including `AccountStores`?
2. What are the effective `ON DELETE` actions in the final EF model/migrations after global conventions run?
3. Can one OrderItem have multiple ProductionIncidents, particularly one per `ProductionUnitNo`?
4. Is `SyncDeadLetter.SyncEventInboxId` unique, optional, and FK-constrained, or can multiple dead letters/evidence rows refer to one inbox event?
5. Does one ProductionPackageInstallation create exactly one ConfigurationRelease, multiple releases/materializations, or only an indirect provenance link?
6. Which AccountRole scope-column combinations are valid, and are they enforced by checks/application validation?
7. Which conceptual entities are true one-to-one physical mappings versus deliberate abstractions over multiple tables?
8. Are append-only/history tables protected against update/delete at the database level or only by application convention?
9. Should `ProductOption.TemplateProductOptionId` become an FK or remain a soft lineage ID?
10. Is `RobotProgram.TemplateProgramId` intentionally absent?
11. Should `EdgeCommandDeliveryAttempts` receive the documented `SentAt` index?
12. Are `ProductionEventCheckpoint` and `EdgeStateSummary` operational upsert projections rather than retention/partition candidates?
13. Which JSON fields have stable schemas, where is each schema version stored, and which are intentionally opaque metadata?
14. Are current retention defaults contractual, configurable operational defaults, or compliance requirements?
15. What is the required soft-delete visibility/restoration policy for each of the 12 query-filter exceptions?
16. Are voucher outcomes part of the current data model, or only future incident-resolution language?
17. Is a Kiosk permitted to exist without an execution endpoint, devices, menu, or robot target during provisioning?
18. Which minimum-child cardinalities are application validation only versus DB-enforced constraints?
19. Are manual migration-step classes mandatory deployment steps, and how are they invoked and audited?
20. Should physical design document enum numeric values used in filtered indexes to protect against enum reordering?

## Final Recommendation

Treat the documents as useful summaries, not yet as an authoritative three-layer database specification.

Before finalization:

1. Define separate counting rules for DbSets, mapped entities, and physical tables.
2. Correct the `DeviceType` key type, capability-projection relationship, and ProductionIncident cardinality.
3. Remove the conceptual one-to-one physical-mapping claim or add an explicit mapping table showing abstractions.
4. Rebuild logical cardinalities from actual FK nullability and unique constraints, distinguishing dependent requiredness from parent minimum cardinality.
5. Reconcile effective delete behavior against the model snapshot or generated migration SQL.
6. Use “non-soft-deleted” for `DeletedAt` filters and preserve every exact composite/partial unique predicate.
7. Expand the logical design to include important omitted entities or label it explicitly as a selected logical summary.
8. Separate supported physical mapping from inferred JSON roles, deployment assumptions, and business rationale.

