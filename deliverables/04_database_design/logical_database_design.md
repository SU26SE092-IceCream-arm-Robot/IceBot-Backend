# Logical Database Design — IceBot Backend

**Document type**: Team-facing database design baseline (working draft), part of the `deliverables/04_database_design/` set. Not the final school-template report.

**Definition used**: Per this task's instructions, logical design covers entities, attributes, identifiers, relationships (cardinality and optionality), normalization-level reasoning, and business constraints — independent of a specific DBMS product's syntax (no PostgreSQL types, index implementation, or EF Core mechanics; those are in `physical_database_design.md`).

**Source basis**: `deliverables/00_repo_evidence/database_inventory.md` §1–§4, §6 (entities, attributes, relationships, constraints, multi-tenancy), `deliverables/02_srs/srs.md` §6 (Data Requirements) and §7 (Business Rules), `deliverables/02_srs/requirements_traceability_matrix.md` (DR-01–DR-16 rows), and `deliverables/03_uml/erd.md`/`class_diagram.md` for relationship shape cross-checking. No `src/` or `docs/` files were read beyond what these evidence documents cite, and none were modified; `srs.md`, `requirements_traceability_matrix.md`, and the UML files were not modified.

**Naming note**: This document uses the entity names as they appear in `database_inventory.md` §1–§2 (e.g. `Product`, `Order`, `RobotProgram`) — singular class/entity names, not the plural physical table names (`Products`, `Orders`, `RobotPrograms`) reserved for `physical_database_design.md`.

**Status tags**: `[Supported]` = directly stated in the cited evidence file. `[Inferred]` = a reasonable reading not independently re-verified line-by-line (matches the source documents' own usage of the term). `[Assumption]` = accepted as current-state without independent verification of intent. `[Unclear]` = a genuine open discrepancy or unresolved question carried from the evidence base.

**Terminology notes (apply throughout this document)**:
- **"Mandatory" / "required" in a relationship description always refers to the dependent (child) side's foreign key being non-nullable** — e.g. "Store, mandatory on the Store side" means a `Store` row cannot exist without an `Organization`. It does **not** mean the parent is guaranteed to have at least one child; unless a relationship is explicitly called out with a minimum-child count and a citation for that minimum, assume the parent's own cardinality is `(0..N)` (a parent may legitimately have zero children).
- **"Non-deleted"**, not "active," is the correct term for rows passing a `DeletedAt IS NULL` filter. This document reserves the word "Active" for an entity's own domain lifecycle status value (e.g. `Kiosk.Status = Active`), which is a materially different concept from "not soft-deleted." Where earlier drafts of this document used "active rows" to mean "non-deleted rows," that wording has been corrected.
- **"Append-only"** describes an entity's base-class naming and its intended application usage pattern (per `database_inventory.md`'s own base-class documentation), **not** a database-enforced immutability mechanism — no trigger, permission revocation, or other DB-level write-protection was found in the evidence for any entity so described. Treat "append-only" entities as `[Inferred]` usage intent, not a `[Supported]` schema guarantee against UPDATE/DELETE.
- Constraints attributed to a `BR-xx` business rule in `srs.md` §7 are **application/domain-code-enforced policies**, not schema-level constraints, unless the same sentence also cites a `database_inventory.md` §4 index/constraint. Constraints attributed only to `database_inventory.md` §4 are schema-enforced (index- or check-constraint-backed). Where a sentence cites both, the schema-enforced part and the application-enforced part are two different claims bundled for narrative convenience, not one uniformly-enforced rule.

---

## 1. Identifier Strategy

`[Supported]` Two surrogate-key patterns are used across the model (`database_inventory.md` §2, §4):
- **Application-generated GUID** identifiers — the default for most business entities (`Organization`, `Store`, `Kiosk`, `Product`, `Order`, etc.). The application assigns the identifier value before the row exists, rather than the database generating it.
- **Database-generated sequential (long integer)** identifiers — used for a smaller set of reference/catalog-style entities (`Role`, `ProductCategory`, `PaymentMethod`, `OptionGroup`, `DeviceType`, and similar `CatalogEntity`-derived types), where the database assigns the value on insert.

In addition to the surrogate primary key, most entities also carry a **business alternate key** — a human-meaningful code or number that must be unique for business reasons independent of the surrogate key. Some of these are unique standalone (`Order.OrderNumber`, `PaymentTransaction.TransactionNumber`, `Refund.RefundNumber`, `MaintenanceTicket.TicketNumber`, `Account.UserName`/`Email`); others are only unique in **combination with additional scope or evidence columns**, and must not be read as unique on their own: `Organization.Code` (unique among non-deleted rows), `Store.Code`/`Kiosk.Code` (unique per Organization, non-deleted rows), `Product.Code`/`RobotProgram.Code` (unique per tenant-scope tuple, non-deleted rows), and `RobotArtifact.ArtifactCode` (unique only as part of the composite `(OrganizationId, ArtifactCode, Checksum)`, not alone). `[Supported]` — `database_inventory.md` §4 ("Soft-delete-aware uniqueness" and the immediately following paragraph).

---

## 2. Entities, Attributes, and Constraints by Subject Area

### 2.1 Tenants

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `Organization` | GUID (surrogate) | `Code` (alternate key, unique among non-deleted rows), `Name`, `LegalName`, `TaxCode`, `Status` | Root of the tenant hierarchy. |
| `Store` | GUID (surrogate) | `OrganizationId` (FK), `Code` (unique per Organization, non-deleted rows), `StoreType`, `Status`, `TimeZone`, `OpeningHours`, sales-pause fields | `OrganizationId` mandatory (non-nullable). |
| `Kiosk` | GUID (surrogate) | `OrganizationId` (FK), `StoreId` (FK), `Code` (unique per Organization, non-deleted rows), `Status`, `OperationalState`, `SerialNumber` | Both `OrganizationId` and `StoreId` mandatory. |
| `FranchiseOnboarding` | GUID (surrogate) | `OrganizationId` (FK), `IdempotencyKey`, `RequestChecksum`, `Status`, `StoreId`/`KioskId` (FK, optional, filled progressively) | Business-process record, not a catalog entity. |

**Relationships**: `Organization` (1) — (0..N) `Store`, mandatory on the Store side (a Store cannot exist without its Organization). `Store` (1) — (0..N) `Kiosk`, mandatory on the Kiosk side. `Organization` (1) — (0..N) `FranchiseOnboarding`, mandatory. `FranchiseOnboarding` (0..1) — (0..1) `Store`/`Kiosk`, optional both ways (the onboarding record may not yet have created its Store/Kiosk). `[Supported]` — `database_inventory.md` §1, §2, §6.

**Business constraints**: A Store's `Code` must be unique within its Organization among non-deleted rows; a Kiosk's `Code` must be unique within its Organization among non-deleted rows. `[Supported]` — `database_inventory.md` §4.

### 2.2 Identity

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `Account` | GUID (surrogate) | `UserName`, `Email` (both alternate keys, unique among non-deleted rows), `Status`, `GoogleSubjectId`/`GoogleEmail` | |
| `Role` | Long (surrogate, DB-generated) | `Code`, `Name`, `IsSystemRole`, `Priority` | Small, mostly static reference set. |
| `AccountRole` | GUID (surrogate) | `AccountId` (FK), `RoleId` (FK), `OrganizationId`/`StoreId`/`KioskId` (all optional FKs — the role's scope), `IsActive` | Represents one role grant at one scope. |
| `AccountInvitation` | GUID (surrogate) | `AccountId` (FK), token hash, expiry, usage/revocation timestamps | Raw token never stored, only its hash. |
| `RefreshToken` | GUID (surrogate) | `AccountId` (FK), token hash, `ReplacedByTokenId` (self-FK, rotation chain), expiry/revocation | |
| `PasswordResetRequest` | GUID (surrogate) | `AccountId` (FK), token hash, expiry/usage | |
| `AccountNotificationDevice` | GUID (surrogate) | `AccountId` (FK), `InstallationId`, `Platform`, push-token hash | |

**Relationships**: `Account` (1) — (0..N) `AccountRole`, mandatory (an `AccountRole` cannot exist without an `Account`). `Role` (1) — (0..N) `AccountRole`, mandatory. `AccountRole.OrganizationId`/`StoreId`/`KioskId` are each (0..1) optional, so a stored row may have any combination of these three populated or null. `[Inferred]` The nullable columns alone establish which combinations can be *stored*, not which combinations are *legally valid* per the application's scope-assignment rules (e.g. whether "Store populated but Organization null" is ever legal) — the exact legal-combination rule was not independently re-verified beyond the nullability itself. `Account` (1) — (0..N) each of `AccountInvitation`, `RefreshToken`, `PasswordResetRequest`, `AccountNotificationDevice`. Additionally, `Account` (0..N) — (0..N) `Store` via the `AccountStores` join table (composite primary key `(AccountId, StoreId)`, no surrogate key and no domain class of its own). `[Supported]` — `database_inventory.md` §1, §2, §3.

**Business constraints**: `Account.UserName`/`Email` unique among non-deleted accounts — schema-enforced (`database_inventory.md` §4). Separately, as an **application-enforced** policy (not a schema constraint): a caller may only assign a Role to another Account if the caller's own Role outranks or equals the target Role in the hierarchy SystemAdmin > OrgAdmin > Manager, and the requested scope must be within the caller's own allowed scope (BR-01, `srs.md` §7).

### 2.3 Catalog

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `ProductCategory` | Long (surrogate) | `Code` | |
| `Product` | GUID (surrogate) | `TemplateProductId` (self-FK, optional lineage), `CategoryId` (FK), `OrganizationId`/`StoreId`/`KioskId` (optional, scope), `Code`, `BasePrice`, `ScopeType` (default Global) | |
| `ProductVariant` | GUID (surrogate) | `ProductId` (FK), `Code`, `VariantType`, `FulfillmentType`, `BasePrice` | |
| `OptionGroup` | Long (surrogate) | `ProductId` (FK), `SelectionType`, `MinSelections`/`MaxSelections` | |
| `ProductOption` | GUID (surrogate) | `OptionGroupId` (FK), `ExecutionImpact` (CommercialOnly/ProductionAffecting), `PriceDelta`, `IsDefault`, `TemplateProductOptionId` (lineage field, present but not enforced as a formal relationship — see §6) | |
| `ProductOptionIngredientRequirement` | `[Unclear]` — exact identifier (surrogate GUID vs. composite PK) not itemized in `database_inventory.md` beyond field names | `ProductOptionId`, `IngredientId` | Links a production-affecting option to the ingredients it needs. |
| `Recipe` | GUID (surrogate) | `ProductVariantId` (FK), `TemplateRecipeId` (self-FK, optional), `Version`, `Status` (Draft/Published/Active/Retired), `IsDefault` | |
| `RecipeItem` | GUID (surrogate) | `RecipeId` (FK), `IngredientId` (FK), `Quantity`, `Unit`, `StepOrder` | |
| `Ingredient` | GUID (surrogate) | `Code`, `IngredientType`, `Unit`, `IsPerishable`, `IsAllergen` | |

**Relationships**: `ProductCategory` (1) — (0..N) `Product`, mandatory. `Product` (1) — (0..N) `ProductVariant`, mandatory. `Product` (1) — (0..N) `OptionGroup`, mandatory. `OptionGroup` (1) — (0..N) `ProductOption`, mandatory. `Product` (0..1) — (0..N) `Product` (self, template lineage), optional. `ProductVariant` (1) — (0..N) `Recipe`, mandatory. `Recipe` (0..1) — (0..N) `Recipe` (self, template lineage), optional. `Recipe` (1) — (0..N) `RecipeItem`, mandatory. `RecipeItem` (0..N) — (1) `Ingredient`, mandatory on the RecipeItem side. `ProductOption` (0..N) — (0..N) `Ingredient` via `ProductOptionIngredientRequirement`. `[Supported]` — `database_inventory.md` §1, §2, §3.

**Business constraints**: At most one `Recipe` may be the non-retired default for a given `ProductVariant` at a time; a Recipe must have at least one required `RecipeItem` before it can be Published; `RecipeItem` replacement is only allowed while the Recipe is Draft; a Recipe is retired, never deleted (BR-07, `srs.md` §7). At most one `ProductOption` may be the default within its `OptionGroup` (BR-12). `[Supported]`.

### 2.4 Sales Catalog

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `Menu` | GUID (surrogate) | `OrganizationId`/`StoreId`/`KioskId` (optional, scope), `Status`, `ScopeType` (default Organization — no Global fallback), `Currency`, `EffectiveFrom`/`EffectiveTo` | |
| `MenuItem` | GUID (surrogate) | `MenuId` (FK), `ProductId` (FK), `ProductVariantId` (FK), `RecipeId` (FK, optional), `Price`, `Status` | |
| `MenuItemProductOption` | `[Unclear]` — exact identifier not itemized beyond field names | `MenuItemId`, `ProductOptionId` | Restricts which options are offered for a given menu item. |

**Relationships**: `Menu` (1) — (0..N) `MenuItem`, mandatory. `MenuItem` (0..N) — (1) `ProductVariant`, mandatory. `MenuItem` (0..N) — (0..1) `Recipe`, optional. `MenuItem` (0..N) — (0..N) `ProductOption` via `MenuItemProductOption`. `[Supported]` — `database_inventory.md` §1, §2.

**Business constraints**: A Menu or MenuItem cannot be set Active unless its referenced product/variant/recipe/options pass a full activation preflight (currency match; active recipe with only active ingredients for machine-produced variants; statically satisfiable option groups) — BR-06, `srs.md` §7. `[Supported]`.

### 2.5 Inventory

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `IngredientDispenserState` | GUID (surrogate) | `DeviceId` (FK), `KioskId` (optional), `IngredientId` (FK), `ContainerCode`, `CurrentLevelStatus`, `EstimatedQuantity`/`CapacityQuantity`, `IsActive` | |
| `StockMovement` | GUID (surrogate) | `IngredientDispenserStateId` (FK), `OrganizationId`/`StoreId`/`KioskId`/`DeviceId` (all optional), `MovementType`, signed `Quantity`, `BalanceBefore`/`BalanceAfter` | Append-only. |
| `InventoryTopologyChangeRecord` | GUID (surrogate) | Before/after container-state snapshot fields | Append-only history. |
| `InventoryTopologyRebindRecord` | GUID (surrogate) | Source/replacement device and ingredient references, `EstimateDisposition`, `TransferredQuantity` | Append-only history. |
| `InventorySensorObservation` | GUID (surrogate) | `KioskExecutionEndpointId`, `SourceExecutorId`, `SourceEventId`, `IngredientDispenserStateId`, `DeviceId`, `IngredientId`, `ObservationSequence`, observed/received times, level, optional derived quantity, `Disposition`, `SensorPayloadJson` | Edge evidence; unique `(SourceExecutorId, SourceEventId)`. Out-of-order evidence is retained without replacing current state. |

**Relationships**: `Device` (1) — (0..N) `IngredientDispenserState`, mandatory. `IngredientDispenserState` (0..N) — (1) `Ingredient`, mandatory. `IngredientDispenserState` (1) — (0..N) `StockMovement` and (0..N) `InventorySensorObservation`; `KioskExecutionEndpoint` (1) — (0..N) `InventorySensorObservation`. `[Supported]` — pre-sync `database_inventory.md` plus backend update impact §4/migration `20260731040709`.

**Business constraints**: At most one active `IngredientDispenserState` container binding per device slot (i.e., per `(DeviceId, ContainerCode)`) — BR-12. A unit change is rejected once a dispenser has an estimated quantity or stock history (requires retirement and a new state instead) — `srs.md` FR-048. `[Supported]`.

### 2.6 Orders

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `Order` | GUID (surrogate) | `KioskId` (FK, mandatory), `OrganizationId`/`StoreId` (optional convenience), `OrderNumber` (alternate key), `Status` (15-value state machine), `PaymentStatus`, money totals | |
| `OrderItem` | GUID (surrogate) | `OrderId` (FK), immutable order-time snapshots (`ProductCodeSnapshot`, `RecipeSnapshotJson`, etc.), `UnitPrice`, `Status` | |
| `OrderItemOption` | GUID (surrogate) | `OrderItemId` (FK), snapshot fields (`OptionGroupCodeSnapshot`, `CodeSnapshot`, `UnitPriceDelta`) | |
| `OrderStatusHistory` | GUID (surrogate) | `OrderId` (FK), transition detail, audit fields | Append-only; fully audited (see §4). |
| `OrderItemStatusHistory` | GUID (surrogate) | `OrderItemId` (FK), transition detail | Append-only; **not** audited the same way as `OrderStatusHistory` (see §4). |
| `ProductionIncident` | GUID (surrogate) | `OrderId`/`OrderItemId` (FK), `Status` (state machine), `InspectionOutcome`, `Resolution` | |
| `ProductionIncidentHistory` | GUID (surrogate) | Transition detail | Append-only; not audited the same way as `OrderStatusHistory`. |

**Relationships**: `Kiosk` (1) — (0..N) `Order`, mandatory. `Order` (1) — (0..N) `OrderItem`, mandatory on the OrderItem side. `[Inferred]` The FK relationship alone only proves every `OrderItem` requires an `Order`; it does not prove every `Order` has at least one `OrderItem` — a business/application rule at order-creation time (`srs.md` FR-057) may require at least one item, but no schema-level minimum-cardinality constraint was found in the evidence, so the base relational cardinality from the Order side is `(0..N)`, not `(1..N)`. `OrderItem` (1) — (0..N) `OrderItemOption`, optional. `Order` (1) — (0..N) `OrderStatusHistory`, mandatory. `OrderItem` (1) — (0..N) `ProductionIncident`, optional (an `OrderItem` may have zero incidents, or more than one over time — e.g. across separate production units or remake attempts; no unique constraint on `OrderItemId` was found limiting it to one). `[Supported]` — `database_inventory.md` §1, §2.

**Business constraints**: An order can only be customer-cancelled while unpaid and non-terminal (`Draft`/`PendingPayment`); a production incident's resolution cannot be selected until its inspection outcome has been recorded (BR-15). `[Supported]`.

### 2.7 Payments

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `PaymentMethod` | Long (surrogate) | `Code`, `Provider`, `MethodType`, `IsOnline` | |
| `PaymentTransaction` | GUID (surrogate) | `OrderId` (FK), `PaymentMethodId` (FK), `TransactionNumber` (alternate key), `Amount`/`PaidAmount`, `Status`, `SettlementDisposition` | |
| `PaymentCallback` | GUID (surrogate) | `PaymentTransactionId` (FK), `EventType`, `ProviderEventId`, `ProcessingStatus` | Append-only evidence. |
| `Refund` | GUID (surrogate) | `PaymentTransactionId` (FK), `RefundNumber` (alternate key), `Amount`, `Status` | |

**Relationships**: `Order` (1) — (0..N) `PaymentTransaction`, optional on the Order side until a payment session is created. `PaymentMethod` (1) — (0..N) `PaymentTransaction`, mandatory. `PaymentTransaction` (1) — (0..N) `PaymentCallback`, optional. `PaymentTransaction` (1) — (0..N) `Refund`, optional. `[Supported]` — `database_inventory.md` §1, §2.

**Business constraints**: At most one `PaymentTransaction` per `Order` may hold the "Primary" settlement disposition (BR-12). Refund rejection, and several other transitions, require a mandatory, non-empty audit reason (BR-11). `[Supported]`.

### 2.8 Devices

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `DeviceType` | Long (surrogate, DB-generated) | `Code` | `database_inventory.md` §7 lists `DeviceTypes` among the `CatalogEntity`-derived, database-generated-long-key tables (alongside `ProductCategories`, `Roles`, `PaymentMethods`, `OptionGroups`) — corrected here from an earlier GUID mislabel. |
| `DeviceModel` | Long (surrogate) | `DeviceTypeId` (FK), `CapabilitiesJson` | |
| `Device` | GUID (surrogate) | `DeviceTypeId` (FK), `DeviceModelId` (FK, optional), `KioskId` (FK, optional), `Code`, `SerialNumber` (alternate key), `Status` | |
| `DeviceEvent` | GUID (surrogate) | `DeviceId`/`KioskId` (FK), `EventId` (business idempotency key), `EventType`, `Severity` | Append-only (usage-pattern claim — see Terminology notes above). |
| `KioskHeartbeat` | GUID (surrogate) | `KioskId`/`NodeId`, `HeartbeatSequence`, `Status` | Append-only (usage-pattern claim). |
| `KioskExecutionEndpoint` | GUID (surrogate) | `KioskId` (FK), `EndpointCode`, `ExecutionProfile` (FullEdge/LowCostController), mutually exclusive `FullEdgeRuntimeId`/`ControllerId` | |
| `ExecutionEndpointMqttCredential` | GUID (surrogate) | `KioskExecutionEndpointId` (FK, 1:1), `Username`, `CredentialVersion`, `Status` | |
| `ExecutionEndpointReadinessProjection` | GUID (surrogate) | `KioskExecutionEndpointId` (FK, 1:1), `StateRevision` (monotonic guard) | Read-model row; `database_inventory.md` §3 explicitly lists this as a true 1:1 with `KioskExecutionEndpoint`. |
| `ExecutionEndpointCapabilityProjection` | GUID (surrogate) | `KioskExecutionEndpointId` (FK), `StateRevision` (monotonic guard) | `[Unclear]` Unlike `ExecutionEndpointReadinessProjection`, this entity is **not** listed in `database_inventory.md` §3's "True 1:1 relationships" enumeration — its exact cardinality relative to the endpoint (one row per endpoint, or one row per endpoint per declared capability) is not established by the cited evidence and should not be assumed to be 1:1. |
| `ExecutionEndpointRequestNonce`, `ExecutionEndpointSupportedRobotTarget` | GUID (surrogate) | `KioskExecutionEndpointId` (FK) | Detailed attributes beyond §1/§3 not itemized in the evidence — `[Unclear]` at attribute level, entity existence is `[Supported]`. |

**Relationships**: `DeviceType` (1) — (0..N) `DeviceModel`, mandatory. `Kiosk` (1) — (0..N) `Device`, optional (a Device may exist unassigned during provisioning, per `KioskId` being nullable — and correspondingly, `Device` (0..1) — (1) `Kiosk` from the Device side is itself optional, not required). `Kiosk` (1) — (0..N) `KioskExecutionEndpoint`, mandatory on the endpoint side (a Kiosk itself may have zero endpoints, e.g. during early provisioning). `KioskExecutionEndpoint` (1) — (0..1) `ExecutionEndpointMqttCredential`, true one-to-one. `KioskExecutionEndpoint` (1) — (0..1) `ExecutionEndpointReadinessProjection`, true one-to-one. `KioskExecutionEndpoint` (1) — (0..N) `ExecutionEndpointCapabilityProjection`, cardinality `[Unclear]` (see table above — may in fact be 1:1 or 1:N; not established). `[Supported]` — `database_inventory.md` §1, §2, §3, except where marked `[Unclear]` above.

**Business constraints**: A `KioskExecutionEndpoint` in the `FullEdge` profile requires `ControllerId IS NULL`; a `LowCostController` profile requires `FullEdgeRuntimeId IS NULL`; an `Active` endpoint must have its profile-matching identity populated. `[Supported]` — `database_inventory.md` §4 (check constraints).

### 2.9 Robot Configuration

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `RobotProgram` | GUID (surrogate) | `OrganizationId`/`StoreId`/`KioskId`/`DeviceId` (optional), `ScopeType`, `Status`, `ProgramManifestJson` | `ValidateScope()` rejects `ScopeType.Global` despite the enum defining it — see §6. |
| `RobotProgramArtifact` | GUID (surrogate) — it is a mapped `RobotConfigurationEntity` (sync-enabled) with its own domain class, not a bare keyless join table | `RobotProgramId` (FK), `RobotArtifactId` (FK), `RunOrder` (> 0) | `[Unclear]` `srs.md` FR-097 states `RunOrder` is unique per artifact/program as a business rule, but `database_inventory.md` §4 does not cite a specific index enforcing this — treat index-level uniqueness as unconfirmed even though the business rule is `[Supported]` at the SRS level. |
| `RobotArtifact` | GUID (surrogate) | `OrganizationId` (mandatory, non-nullable), `ArtifactCode` (part of a composite unique key together with `OrganizationId` and `Checksum` — **not** unique alone), `Checksum`, `Status`, `TechnicalContractId` (optional) | |
| `RobotArtifactTemplate` | GUID (surrogate) | Same shape as `RobotArtifact` but global (no `OrganizationId`) | |
| `RobotArtifactTechnicalContract` | GUID (surrogate) | `OrganizationId` (optional — null means global), `ContractCode`/`ContractVersion`, `SchemaVersion`, `Status` | |
| `RobotArtifactDeclaredEffect`, `RobotArtifactOrderingConstraint` | GUID (surrogate) | Child rows of a Technical Contract | |
| `RobotAuthoringImport` | GUID (surrogate) | `OrganizationId` (mandatory), `ClientExportId`, `ImportChecksum`, `IdempotencyKey`, `Status` | |
| `RobotAuthoringImportItem` | GUID (surrogate) | `RobotAuthoringImportId` (FK), `ArtifactCode`, `RunOrder`, `Status` | |

**Relationships**: `RobotProgram` (1) — (0..N) `RobotProgramArtifact`. The base relational cardinality is `(0..N)` — a Draft `RobotProgram` can have zero artifacts before authoring is complete; "at least one artifact, in RunOrder sequence" is an application-level publish-time rule (`srs.md` FR-097), not a schema-enforced minimum. `RobotProgramArtifact` (0..N) — (1) `RobotArtifact`, mandatory on the RobotProgramArtifact side. `RobotArtifact` (0..N) — (0..1) `RobotArtifactTechnicalContract`, optional. `RobotArtifactTechnicalContract` (1) — (0..N) `RobotArtifactDeclaredEffect`/`RobotArtifactOrderingConstraint`, mandatory on the child side (a contract may legitimately have zero declared effects/constraints; the FK from child to parent is what is required, not a minimum count on the parent). This child relationship is configured with an explicit `Cascade` delete-behavior override in its own configuration class, but whether that override survives `IceBotDbContext`'s later global `Restrict` convention loop is `[Unclear]` — see `physical_database_design.md` §2 and Open Questions. `RobotAuthoringImport` (1) — (0..N) `RobotAuthoringImportItem`, mandatory on the child side. `[Supported]` — `database_inventory.md` §1, §2, §3, except where marked `[Unclear]`.

**Business constraints**: A Draft `RobotArtifact` requires verified checksum/size. A technical declaration is optional; when referenced it must be Published, scope/target compatible, and checksum-consistent. It is not behavior certification. `[Supported]` — backend update impact §5.

### 2.10 Production Configuration / Production Execution

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `ConfigurationRelease` | GUID (surrogate) | `OrganizationId` (mandatory), `ReleaseNumber` (alternate key, sequential per organization), `Status`, `ManifestJson` | No Store/Kiosk/Device columns directly — see §6. |
| `ExecutionRoute` | GUID (surrogate) | `ConfigurationReleaseId` (FK), `ProductVariantId`/`RecipeId` (FK), `RouteCode`, `Priority` | |
| `ExecutionRouteRobotBinding` | `[Unclear]` — exact identifier not itemized beyond field names | `ExecutionRouteId`, `RobotProgramId` | |
| `ProductionProgramBinding` | GUID (surrogate) | `OrganizationId`, `ProductVariantId`, `RecipeId`, `RecipeVersion`, `RobotProgramId`, program/binding checksums, supported-option JSON, required-capability JSON, `CapabilityEvidenceStatus`, `Assurance`, `Status`, retirement/audit/soft-delete fields | Immutable operator-confirmed binding; unique binding checksum. |
| `KioskConfigurationDeployment` | GUID (surrogate) | `KioskId`/`OrganizationId`/`KioskExecutionEndpointId`/`ConfigurationReleaseId` (FK), `AttemptNo`, `Status` | Full-Edge deployment path. |
| `ControllerArtifactSetDeployment`/`Item` | GUID (surrogate) | Mirrors `KioskConfigurationDeployment` with `ControllerId` | Low-cost-controller path. |
| `OrderExecutionRecord` | GUID (surrogate) | `SourceCommandId`/`KioskExecutionEndpointId` (FK), `Status`, idempotency via `(SourceExecutorId, LastAppliedSourceEventId)` | Cloud-side audit projection, not a live scheduler. |
| `ProductionExecutionRecord` | GUID (surrogate) | Same idempotency pattern, plus `PhysicalOutputState`, `OrderItemId`/`ProductionUnitNo` | |

**Relationships**: `ConfigurationRelease` (1) — (0..N) `ExecutionRoute`, mandatory. `ExecutionRoute` references one ProductVariant and Recipe and links RobotPrograms through `ExecutionRouteRobotBinding`. A ProductionProgramBinding references one Organization, ProductVariant, Recipe, and RobotProgram; an ExecutionRouteRobotBinding may reference one ProductionProgramBinding and snapshot its checksum/capability array. `[Supported]` — pre-sync `database_inventory.md` plus backend update impact §4/migrations `20260804031725`, `20260809035315`.

**Business constraints**: A `ConfigurationRelease` can only be published after route/binding validation and a passing inventory-readiness check (BR-09). At most one Pending/Installed `KioskConfigurationDeployment` per kiosk at a time (BR-12). `[Supported]`.

### 2.11 Production Packages

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `ProductionPackage` | GUID (surrogate) | `Code` | Platform-level, SystemAdmin-owned. |
| `ProductionPackageVersion` | GUID (surrogate) | `ProductionPackageId` (FK), `ManifestJson` | Immutable once Published. |
| `ProductionPackageProductDefinition`, `...ArtifactDefinition`, `...ProgramBlueprint`(+`Slot`), `...RouteBlueprint` | GUID (surrogate) | Children of a Version, defining what it materializes | Attribute detail beyond §1 not itemized in the evidence for every field — entity existence is `[Supported]`, exhaustive attribute list is `[Unclear]`. |
| `ProductionPackageInstallation` | GUID (surrogate) | `OrganizationId` (FK), `ProductionPackageVersionId` (FK), `IdempotencyKey` | |
| `ProductionPackageMaterialization`, `ProductionComposition` | GUID (surrogate) | Records of what was actually created/reused during an installation | |
| `ProductionPackageUpgrade` (+ 6 child tables) | GUID (surrogate) | `OrganizationId`, `SourceInstallationId`, `Status` | Full upgrade sub-family listed in `database_inventory.md` §1; not individually detailed here for readability. |

**Relationships**: `ProductionPackage` (1) — (0..N) `ProductionPackageVersion`, mandatory on the Version side. `ProductionPackageVersion` (1) — (0..N) `ProductionPackageInstallation`, optional. `[Supported]` — `database_inventory.md` §1, §2. `[Inferred]` A `ProductionPackageInstallation` is described as materializing organization data, which in practice results in a `ConfigurationRelease`; `database_inventory.md` §3's notable-relationship list does not cite a direct FK or index between `ProductionPackageInstallation` and `ConfigurationRelease`, so this is a workflow-level inference, not a confirmed one-to-zero-or-one FK relationship — it could equally be an indirect provenance link or involve more than one release over repeated materializations.

**Business constraints**: Once published, a `ProductionPackageVersion`'s manifest is immutable — installations/upgrades reference it by exact version (BR-10). At most one active (non-terminal) `ProductionPackageUpgrade` per installation (BR-12). `[Supported]`.

### 2.12 Operations

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `Alert` | GUID (surrogate) | `KioskId`/`DeviceId` (optional), `AlertCode`, `CorrelationKey`, `Severity`, `Status` | Deduplicated by correlation key. |
| `MaintenanceTicket` | GUID (surrogate) | `OrganizationId`/`StoreId`/`KioskId` (all mandatory), `TicketNumber` (alternate key), `Priority`, `Status`, `OperationalImpact` | |
| `OperationLog` | GUID (surrogate) | `AccountId`/`KioskId`/`DeviceId`/`OrderId` (all optional evidence links), `Action`, `Category` | Append-only. |
| `NotificationDelivery` | GUID (surrogate) | `OrganizationId` (mandatory), `StoreId`/`KioskId` (optional), `DeliveryKey`, `RecipientAccountId`, `Status` | |

**Relationships**: `Kiosk` (1) — (0..N) `Alert`, mandatory. `Device` (0..1) — (0..N) `Alert`, optional. `Kiosk` (1) — (0..N) `MaintenanceTicket`, mandatory. `Account` (0..1) — (0..N) `NotificationDelivery`, optional (recipient). `[Supported]` — `database_inventory.md` §1, §2.

### 2.13 Sync

| Entity | Identifier | Key Attributes | Notes |
|---|---|---|---|
| `SyncEventInbox` | GUID (surrogate) | `EventId`, `KioskId` (optional), `SequenceNumber`, `Status` (6-state incl. DeadLettered) | Bare entity, no audit columns. |
| `SyncDeadLetter` | GUID (surrogate) | `SyncEventInboxId`/`EventId`/`KioskId` (optional), `ErrorMessage`, `Status` | |
| `SyncDeadLetterRetryAttempt` | GUID (surrogate) | `SyncDeadLetterId` (FK), `AttemptNumber`, `Succeeded` | |
| `EdgeCommand` | GUID (surrogate) | `KioskId`, `TargetExecutionEndpointId` (FK), `CommandType` (ExecuteOrder/DeployConfiguration), `Status` | |
| `EdgeCommandDeliveryAttempt` | GUID (surrogate) | `EdgeCommandId` (FK), `DeliveryAttemptNo`, `SentAt`, `Outcome` | |
| `ProductionEventCheckpoint`, `EdgeStateSummary` | GUID (surrogate) | Unique per executor (or per executor+kind) | Bounded upsert rows, not append-only logs — see §6. |

**Relationships**: `[Unclear]` `SyncEventInbox` and `SyncDeadLetter` are both listed in `database_inventory.md` §1/§2 with a nullable `SyncEventInboxId`/`EventId` linkage, and the workflow narrative (a failed inbox event produces a dead letter) suggests a near-1:1 relationship — but `database_inventory.md` §3's notable-relationship list does not cite this as a true 1:1 (unlike, e.g., `KioskExecutionEndpoint ↔ ExecutionEndpointMqttCredential`), and no unique index on `SyncDeadLetter.SyncEventInboxId` is cited. This document does not assert an exact cardinality for `SyncEventInbox` ↔ `SyncDeadLetter` pending that confirmation. `SyncDeadLetter` (1) — (0..N) `SyncDeadLetterRetryAttempt`, optional. `[Supported]`. `KioskExecutionEndpoint` (1) — (0..N) `EdgeCommand`, mandatory on the EdgeCommand side. `EdgeCommand` (1) — (0..N) `EdgeCommandDeliveryAttempt`, optional. `[Supported]` — `database_inventory.md` §1, §2, §3.

---

## 3. Cross-Cutting Business Constraints

These recur across multiple subject areas and are stated once here (matching `srs.md` §7's own approach):

- **Tenant-scope consistency**: several cross-context relationships are structurally required to stay within the same tenant (e.g., a `DeviceEvent` must belong to a `Device` that belongs to the same `Kiosk`; an `EdgeCommand`'s target endpoint must belong to the same `Kiosk`; a deployment's release must belong to the same `Organization` as the kiosk it targets). `[Supported]` for the specific relationships enumerated in `database_inventory.md` §3; `[Unclear]` whether an equivalent guarantee holds for cross-tenant references not covered by that enumerated set (`srs.md` NFR-023).
- **Soft-delete-aware uniqueness**: reusable business codes are unique only among non-deleted rows, so a code can be reused after its original row is soft-deleted — except for evidence/retry-critical keys (`Order.OrderNumber`, `PaymentTransaction.TransactionNumber`, `Refund.RefundNumber`), which remain unique even across soft-deleted rows (though not necessarily forever — physical deletion or a future migration change could still free the value). `[Supported]` — `database_inventory.md` §4.
- **"One active per slot" invariants**: six distinct business rules each require at most one active/default/primary row per some grouping key — default product option per group, default non-retired recipe per variant, primary-settlement payment transaction per order, pending/installed deployment per kiosk, active upgrade per installation, active container binding per device slot (BR-12). Each is its own distinct rule with its own grouping key — they should not be read as one generalized "active flag" pattern. `[Supported]`.
- **Restrictive referential integrity by default**: deleting a row is blocked by default if dependent rows reference it, with a small set of documented exceptions where a parent genuinely owns its children (e.g., a Technical Contract owns its Declared Effects). `[Supported]` for the default rule; `[Unclear]` whether the documented owned-child exceptions are fully honored at the physical level — see `physical_database_design.md`.
- **Soft-delete exceptions for principal types**: twelve entities (`Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, `KioskExecutionEndpoint`) are excluded from automatic soft-delete filtering at read time because other entities hold required references to them that must remain visible even after a "delete" (BR-05). `[Supported]`; `[Inferred]` this is a developer-responsibility convention rather than an independently audited guarantee that every query honors it.

---

## 4. Normalization-Level Reasoning

`[Inferred]` (a synthesis across `database_inventory.md`, not a single explicit statement in the evidence): the schema is consistent with a **normalized (3NF-oriented) design for transactional/master data**, with **deliberate, well-scoped denormalization** in two places:

1. **Order-time snapshots** (`OrderItem.ProductCodeSnapshot`/`RecipeSnapshotJson`, `OrderItemOption`'s snapshot fields) intentionally duplicate catalog data at the moment of purchase, rather than normalizing purely by foreign key to `Product`/`ProductVariant`/`Recipe`. This is a standard "capture history" denormalization: it trades update-anomaly risk (none, since these fields are never updated after creation) for audit correctness — a later catalog price change must not retroactively alter a historical order's recorded price. `[Supported]` — `database_inventory.md` §2 (Orders), §5 (Role 2 — Immutable order/execution-time snapshot).
2. **`*Json` columns** (source-of-truth configuration, snapshots, evidence payloads, and metadata — four distinct roles per `database_inventory.md` §5) are a controlled denormalization/schema-flexibility mechanism: they let semi-structured or evolving payloads (robot program manifests, provider raw request/response bodies, sensor telemetry) live alongside strictly-typed relational columns without a fully normalized sub-schema for every nested structure. This is consistent with `docs/data/JSON_FIELD_RULES.md` as cited in the evidence, not an ad hoc choice. `[Supported]`.

Everything else in the model — reference/catalog tables (`Ingredient`, `DeviceType`, `Role`), one-to-many aggregate structures (`Order`→`OrderItem`, `Recipe`→`RecipeItem`, `RobotProgram`→`RobotProgramArtifact`), and many-to-many relationships resolved through explicit join entities (`Account`↔`Store`, `ProductOption`↔`Ingredient`) rather than repeating-group columns — follows conventional normalization practice. `[Inferred]` No explicit normal-form claim (1NF/2NF/3NF) is stated anywhere in the evidence base; this section is this document's own synthesis from the structural evidence, not a quoted source claim.

---

## 5. Evidence Notes

- Entity list, identifiers, and per-context attributes: `database_inventory.md` §1, §2.
- Relationship shapes (composite tenant-consistency FKs, true 1:1s, self-referencing lineage FKs, many-to-many join entities, unowned/shadow FKs): `database_inventory.md` §3.
- Constraints (soft-delete-aware uniqueness, partial/filtered unique indexes, check constraints): `database_inventory.md` §4 — restated here in database-agnostic business-rule language; exact index/predicate syntax is deferred to `physical_database_design.md`.
- Multi-tenancy scope model (`TenantScopeType`, override hierarchy, `RobotProgram`'s Global-rejection exception, lack of a blanket tenant query filter): `database_inventory.md` §6.
- Business rules (BR-01 through BR-15): `srs.md` §7, cross-referenced against the same evidence rows.
- Cross-checked relationship shape against `class_diagram.md` and `erd.md`, including the two post-sync entities.

---

## 6. Open Questions

- `[Open Question]` `ProductOption.TemplateProductOptionId` exists as a data field but has no enforced relationship in code, unlike `Product.TemplateProductId`/`Recipe.TemplateRecipeId` — whether this is an intentional soft reference or an oversight is not resolved (`database_inventory.md` §9 item 5).
- `[Open Question]` `RobotProgram` is documented elsewhere as using a `TemplateProgramId` lineage field like `Product`/`Recipe`, but no such field was found in the evidence — either it lives in an unread part of the source or the documentation is stale (`database_inventory.md` §9 item 1).
- `[Open Question]` Whether the explicit "owned child" exceptions to the default restrictive delete rule (Technical Contract → Declared Effects, Authoring Import → Items, Production Package parent → child) are actually honored, or silently overridden back to the restrictive default by a later configuration step, was not settled by evidence review alone (`database_inventory.md` §9 item 6; `srs.md` NFR-004) — this is a logical-design-relevant open question because it affects whether deleting a parent aggregate is expected to cascade or be blocked.
- `[Open Question]` Whether `Order` → `OrderItem` truly enforces "at least one item" as a database-level rule, or only as application-layer validation at order-creation time, was not independently verified — the evidence documents the entities and their FK relationship but not an explicit minimum-cardinality database constraint. This document now models the base relational cardinality as `(0..N)` from the Order side accordingly.
- `[Open Question]` The exact business meaning of a `ConfigurationRelease` having no direct Store/Kiosk/Device columns (scope living one level down on `ExecutionRoute`/`Recipe`/`RobotProgram` instead) versus the multi-tenancy documentation's suggested broader composite scope is a known discrepancy (`database_inventory.md` §9 item 2) that a future logical-design revision should resolve with the team.
- `[Open Question]` Whether `TenantScopeType`'s `Device > Kiosk > Store > Organization > Global` resolution order is intended to apply uniformly, given `RobotProgram`'s documented exception, was not settled — see `srs.md` §6.6 and `requirements_traceability_matrix.md` DR-15.
- `[Open Question]` What is `ExecutionEndpointCapabilityProjection`'s true cardinality relative to `KioskExecutionEndpoint` — a 1:1 read-model row per endpoint (like `ExecutionEndpointReadinessProjection`), or a 1:N collection of per-capability rows? `database_inventory.md` groups the two projections under one descriptive sentence in §2 but only lists `ExecutionEndpointReadinessProjection` in §3's explicit "True 1:1" enumeration — this document does not resolve the ambiguity.
- `[Open Question]` Is `SyncDeadLetter.SyncEventInboxId` unique, optional, and FK-constrained (implying a true 1:1 with `SyncEventInbox`), or can multiple dead-letter/evidence rows reference one inbox event? Not established by the cited relationship evidence.
- `[Open Question]` Does one `ProductionPackageInstallation` create exactly one `ConfigurationRelease`, multiple releases/materializations over its lifetime, or only an indirect provenance link? Not established by the cited relationship evidence.
- `[Open Question]` Which specific combinations of `AccountRole.OrganizationId`/`StoreId`/`KioskId` (all-null, one populated, two populated, all three populated) are legally valid per the application's scope-assignment rules, and are any of them enforced by a database check constraint or only by application code? Not established by the cited evidence.
- `[Open Question]` Can one `OrderItem` legitimately have multiple concurrently-open `ProductionIncident` rows, or does application logic restrict it to one open incident at a time even though the schema does not? Not established by the cited evidence — this document models the schema-level cardinality only (`0..N`, no open/concurrent limit asserted).
- `[Open Question]` Are append-only/history tables (`OrderStatusHistory`, `StockMovement`, `DeviceEvent`, etc.) protected against UPDATE/DELETE at the database level (e.g. via a trigger or restricted grant), or only by application convention and base-class naming? Not established by the cited evidence — see the Terminology notes in this document's header.
