# Physical Database Design — IceBot Backend

**Document type**: Team-facing database design baseline (working draft), part of the `deliverables/04_database_design/` set. Not the final school-template report.

**Definition used**: Per this task's instructions, physical design covers actual implementation-oriented tables/entities, keys, indexes, constraints, PostgreSQL/EF Core-specific notes, soft-delete behavior, unique indexes, and known implementation details — the DBMS- and ORM-specific layer beneath `logical_database_design.md`.

**Source basis**: `deliverables/00_repo_evidence/database_inventory.md` (all sections; this document is the primary source), `deliverables/02_srs/srs.md` §6 and §8.6 (data requirements and open items), `deliverables/02_srs/requirements_traceability_matrix.md` DR-13/DR-16, and `deliverables/03_uml/erd.md` for cross-checking the physical table set already trimmed there. No `src/` or `docs/` files were read beyond what these evidence documents cite, and none were modified; `srs.md`, `requirements_traceability_matrix.md`, and the UML files were not modified.

**Naming note**: This document uses the plural physical table names exactly as they appear in `database_inventory.md` §1 (e.g. `Organizations`, `Orders`, `RobotPrograms`) and the C# entity/property names for columns, since these are implementation identifiers, not business concepts.

**Status tags**: `[Supported]` = directly stated in `database_inventory.md` with cited file/line evidence. `[Inferred]` = a reasonable reading not independently re-verified line-by-line (as flagged in the source itself). `[Assumption]` = a current-configuration fact treated as the deployment baseline without independent verification that it is a binding requirement. `[Unclear]` = a genuine open discrepancy carried from `database_inventory.md` §9.

---

## 1. Platform and Provider

- **Database engine**: PostgreSQL 17 (`docker/docker-compose.yml`, `image: postgres:17`), database name `IceBotDB`. `[Assumption]` — this is the current deployment configuration, not independently confirmed as a binding "must be PostgreSQL 17" product requirement (`srs.md` §2.4).
- **Driver/ORM**: Entity Framework Core via the Npgsql provider (`UseNpgsql`), registered both at runtime (`src/Infrastructure/DependencyInjection.cs:85`) and at design time (`src/Infrastructure/AppDbContextFactory.cs:31-35`, which also enables `EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)`). `[Supported]`.
- **DbContext**: `IceBotDbContext` (`src/Infrastructure/Data/IceBotDbContext.cs`), exposing **98** `DbSet<T>` properties — a direct count against source lines 109–214, correcting `database_inventory.md`'s own stated "~130" (per `srs.md` §6.1 and `requirements_traceability_matrix.md` DR-16). `[Supported]`. `[Unclear]` **This is a count of mapped entity types (DbSets), not a count of physical database tables**, and the two are not interchangeable here: `database_inventory.md` §1 also lists `AccountStores` as a physical join table with no corresponding `DbSet`/domain class (it is reached only via EF's many-to-many mapping between `Account` and `Store`). Under a physical-table counting rule, the table count is therefore **at least 99**, not 98. This document uses "98" only to mean "mapped entity types" and does not equate it with the physical table count — see §3.
- **Connection configuration**: runtime resolves the connection string from configuration key `CONNECTIONSTRING`; the design-time factory (used by `dotnet ef migrations`) resolves it from `ConnectionStrings:IceBot_DB` / environment variable `ConnectionStrings__IceBot_DB`. These are two different keys for the same physical database — `[Unclear]`/flagged risk for anyone debugging an environment-specific connection issue, not independently resolved here. `[Supported]` as an observed fact; `[Unclear]` whether this divergence is intentional.

---

## 2. Global EF Core Conventions

`[Supported]` — all of the following are enforced globally by `IceBotDbContext.ConfigureConventions`/`ConfigureEntityConventions` (`IceBotDbContext.cs:216-273`), not configured per entity:

| Convention | Rule |
|---|---|
| Decimal precision | Every `decimal` property maps to `precision(18,4)` unless explicitly overridden. |
| String length | Every `string` property defaults to `maxlength(500)` unless explicitly overridden. |
| JSON mapping | Every `string` property whose name ends in `"Json"` maps to a PostgreSQL `jsonb` column, by reflection over the naming convention — not a per-field opt-in (see §5). |
| GUID key generation | `GuidEntity.Id` is `ValueGeneratedNever()` — the application assigns the GUID before insert, the database does not generate it. |
| Long key generation | `LongEntity.Id` is `ValueGeneratedOnAdd()` — the database assigns the value via identity/sequence (used by `CatalogEntity`-derived tables: `ProductCategories`, `Roles`, `PaymentMethods`, `OptionGroups`, `DeviceTypes`, and similar). |
| Delete behavior | Every foreign key's `DeleteBehavior` is force-set to `Restrict` by a global loop that runs **after** all `IEntityTypeConfiguration<T>` classes have applied their own configuration. |
| Sync entity indexing | Any entity implementing `IRobotSyncEntity` (i.e. `AppendOnlySyncEntity`/`SyncAggregateEntity`/`RobotConfigurationEntity` subclasses) automatically gets a composite index on `(OriginNodeId, Version)`. |
| Tenant indexing | Any entity implementing `IOrganizationScoped` automatically gets a plain index on `OrganizationId`. |

`[Unclear]` A small number of configuration classes explicitly set `DeleteBehavior.Cascade` for genuine parent-owns-child pairs (`RobotArtifactTechnicalContract → RobotArtifactDeclaredEffect/OrderingConstraint`, `RobotAuthoringImport → RobotAuthoringImportItem`, `ProductionPackage*` parent/child pairs, `ControllerArtifactSetDeployment → Item`) **before** the global `Restrict` loop runs. Whether the later loop silently reverts these back to `Restrict` was not settled by static reading of the C# source alone (`database_inventory.md` §3, §9 item 6) — this remains open in Physical Design as it directly affects expected delete behavior; see Open Questions.

---

## 3. Table Inventory (Physical Names)

`[Unclear]` Three different counting rules appear across the evidence base, and this document does not treat them as equivalent: **98** is the count of `DbSet<T>` properties/mapped entity types (§1); the table list below, following `database_inventory.md` §1, additionally includes `AccountStores` as a physical join table without its own `DbSet`, making the physical table count **at least 99** under a table-counting rule; separately, §6 below cites a cumulative migration `CreateTable` count of approximately 99, which is a count of creation events across migration history, not an independently-verified current live-schema count — the two "99" figures are not necessarily the same number for a different reason (one is a name-level table count, the other is a historical creation-event count) even though they happen to be close. None of these three counts has been reconciled against a live `IceBotDbContextModelSnapshot` or an actual running database in this evidence pass. Tables with attribute-level evidence in `database_inventory.md` §2 are detailed in §4 below; the remaining tables are listed here by name only (per §1 of the source) to avoid inventing column-level detail not present in the evidence.

| Bounded context (EF configuration folder) | Physical tables |
|---|---|
| Tenants | `Organizations`, `Stores`, `Kiosks`, `KioskOperationalStateTransitions`, `FranchiseOnboardings` |
| Identity | `Accounts`, `AccountRoles`, `AccountNotificationDevices`, `AccountInvitations`, `PasswordResetRequests`, `RefreshTokens`, `Roles`, `AccountStores` (join, no domain class) |
| Catalog | `ProductCategories`, `Products`, `ProductVariants`, `OptionGroups`, `ProductOptions`, `ProductOptionIngredientRequirements`, `Recipes`, `RecipeItems`, `Ingredients` |
| Inventory | `IngredientDispenserStates`, `InventoryTopologyChangeRecords`, `InventoryTopologyRebindRecords`, `StockMovements` |
| SalesCatalog | `Menus`, `MenuItems`, `MenuItemProductOptions` |
| Orders | `Orders`, `OrderItems`, `OrderItemOptions`, `OrderItemOptionIngredientRequirements`, `OrderStatusHistories`, `OrderItemStatusHistories`, `ProductionIncidents`, `ProductionIncidentHistories` |
| Payments | `PaymentMethods`, `PaymentTransactions`, `PaymentCallbacks`, `Refunds` |
| Devices | `DeviceTypes`, `DeviceModels`, `Devices`, `DeviceEvents`, `KioskHeartbeats`, `KioskConnectivityProjections`, `KioskExecutionEndpoints`, `ExecutionEndpointCredentialBindings`, `ExecutionEndpointMqttCredentials`, `ExecutionEndpointReadinessProjections`, `ExecutionEndpointCapabilityProjections`, `ExecutionEndpointRequestNonces`, `ExecutionEndpointSupportedRobotTargets` |
| RobotConfiguration | `RobotPrograms`, `RobotProgramArtifacts`, `RobotArtifacts`, `RobotArtifactTemplates`, `RobotArtifactTechnicalContracts`, `RobotArtifactDeclaredEffects`, `RobotArtifactOrderingConstraints`, `RobotAuthoringImports`, `RobotAuthoringImportItems` |
| ProductionConfiguration / ProductionExecution / ProductionPackages | `ConfigurationReleases`, `ExecutionRoutes`, `ExecutionRouteRobotBindings`, `KioskConfigurationDeployments`, `ControllerArtifactSetDeployments`, `ControllerArtifactSetItems`, `OrderExecutionRecords`, `ProductionExecutionRecords`, `ProductionPackages`, `ProductionPackageVersions`, `ProductionPackageProductDefinitions`, `ProductionPackageArtifactDefinitions`, `ProductionPackageProgramBlueprints`, `ProductionPackageProgramSlots`, `ProductionPackageRouteBlueprints`, `ProductionPackageInstallations`, `ProductionPackageMaterializations`, `ProductionCompositions`, `ProductionPackageUpgrades`, `ProductionPackageUpgradeMenuChanges`, `ProductionPackageUpgradeMenuOptionChanges`, `ProductionPackageUpgradeEndpointTargets`, `ProductionPackageUpgradeRollbackAttempts`, `ProductionPackageUpgradeCatalogIdentityChanges`, `ProductionPackageUpgradeAvailabilityChanges` |
| Operations | `Alerts`, `MaintenanceTickets`, `OperationLogs`, `NotificationDeliveries` |
| Sync | `SyncEventInbox` (singular table name, note the exception), `ProductionEventCheckpoints`, `EdgeStateSummaries`, `SyncDeadLetters`, `SyncDeadLetterRetryAttempts`, `EdgeCommands`, `EdgeCommandDeliveryAttempts` |

`[Supported]` — `database_inventory.md` §1; migration table names cross-confirmed via `migrationBuilder.CreateTable` calls across the 5 migrations listed in §6 below.

---

## 4. Detailed Table Design (Keys, Indexes, Constraints)

Only tables with attribute/constraint-level evidence in `database_inventory.md` §2–§4 are detailed here; this is not a claim that other tables in §3 lack similar structure, only that their column-level detail was not itemized in the evidence base.

### 4.1 Soft-delete-aware unique indexes

`[Supported]` — `EfModelConfigurationConstants.ActiveRowFilter = "\"DeletedAt\" IS NULL"` backs a `NotNullAndActive(col)` helper used to build unique indexes filtered to non-deleted rows only, verified present for:

`Organization.Code`; `Store.(OrganizationId, Code)`; `Kiosk.(OrganizationId, Code)` and `Kiosk.SerialNumber`; `Account.UserName`/`Email`/`GoogleSubjectId`; `Product.(scope…, Code)`; `Recipe.(scope…, ProductVariantId, Code, Version)`; `Menu.(scope…, Code)`; `MenuItem.(MenuId, Code)`; `RobotArtifact.(OrganizationId, ArtifactCode, Checksum)`; `RobotProgram.(scope…, Code)`; `Device.(KioskId, Code)` and `Device.SerialNumber`; `MaintenanceTicket.TicketNumber`.

### 4.2 Unfiltered (evidence/retry-critical) unique indexes

`[Supported]` — deliberately **not** filtered by `DeletedAt`, so uniqueness holds even across soft-deleted rows: `PaymentTransaction.TransactionNumber`; `Refund.RefundNumber`; `Order.OrderNumber`; `FranchiseOnboarding.RequestChecksum`/`IdempotencyKey` (the composite idempotency key is filtered by active status; the checksum alone is not).

### 4.3 Partial/filtered unique indexes enforcing a business invariant

`[Supported]` — six distinct rules, each with its own filter predicate (do not generalize across them):

| Table | Unique on | Filter predicate | Invariant |
|---|---|---|---|
| `ProductOptions` | `OptionGroupId` | `"IsDefault" = TRUE AND "DeletedAt" IS NULL` | At most one default option per group. |
| `Recipes` | `ProductVariantId` | `"IsDefault" = TRUE AND "Status" <> 4 AND "DeletedAt" IS NULL` | At most one default, non-retired recipe per variant. |
| `PaymentTransactions` | `OrderId` | `"SettlementDisposition" = 1` | At most one Primary-settlement transaction per order. |
| `KioskConfigurationDeployments` | `KioskId` | `"Status" IN (1, 2)` | At most one Pending/Installed deployment per kiosk. |
| `ProductionPackageUpgrades` | `(OrganizationId, SourceInstallationId)` | `"DeletedAt" IS NULL AND "Status" IN (0,1,2,3)` | At most one active (non-terminal) upgrade per installation. |
| `IngredientDispenserStates` | `(DeviceId, ContainerCode)` | `"IsActive" = TRUE AND "DeletedAt" IS NULL` | One active container binding per device slot. |

`[Unclear]` Three of these predicates embed numeric enum values (`"Status" <> 4` for Recipes; `"SettlementDisposition" = 1` for PaymentTransactions; `"Status" IN (1, 2)` for KioskConfigurationDeployments; `"Status" IN (0,1,2,3)` for ProductionPackageUpgrades) exactly as documented in `database_inventory.md` §4. This document does not independently re-map each numeric literal back to its named enum member (e.g., confirming that `RecipeStatus` value `4` is specifically `Retired`) beyond what `database_inventory.md` itself states inline, and does not assert that these numeric values are stable against future enum reordering — a physical-design risk worth flagging for the team rather than silently assuming.

### 4.4 Check constraints

`[Supported]` — rare; most invariants are enforced in domain/application code, not the database:

- `CK_KioskExecutionEndpoints_ProfileIdentity` on `KioskExecutionEndpoints`: `FullEdge` profile requires `ControllerId IS NULL`; `LowCostController` profile requires `FullEdgeRuntimeId IS NULL`; an `Active` endpoint must have its profile-matching identity populated.
- `CK_ProductionPackageInstallations_KioskRequiresStore` on `ProductionPackageInstallations`: `KioskId IS NULL OR StoreId IS NOT NULL`.

### 4.5 Composite tenant-consistency foreign keys

`[Supported]` — these FKs reference a composite key on the parent side, making it structurally impossible to persist a cross-tenant row for the relationship (not merely a documented convention):

| Child (FK columns) | Parent (referenced composite) |
|---|---|
| `DeviceEvents.(DeviceId, KioskId)` | `Devices.(Id, KioskId)` |
| `ExecutionEndpointSupportedRobotTargets.(*, KioskId)` | `KioskExecutionEndpoints.(Id, KioskId)` and `Devices.(Id, KioskId)` |
| `EdgeCommands.(TargetExecutionEndpointId, KioskId)` | `KioskExecutionEndpoints.(Id, KioskId)` |
| `OrderExecutionRecords`/`ProductionExecutionRecords.(SourceCommandId, KioskExecutionEndpointId)` | `EdgeCommands.(Id, TargetExecutionEndpointId)` |
| `KioskConfigurationDeployments`/`ControllerArtifactSetDeployments.(ConfigurationReleaseId, OrganizationId)` | `ConfigurationReleases.(Id, OrganizationId)` |
| `KioskConfigurationDeployments`/`ControllerArtifactSetDeployments.(KioskId, OrganizationId)` | `Kiosks.(Id, OrganizationId)` |
| `NotificationDeliveries.(StoreId, OrganizationId)` / `(KioskId, OrganizationId)` | `Stores.(Id, OrganizationId)` / `Kiosks.(Id, OrganizationId)` (nullable-safe) |
| `ProductionEventCheckpoints`/`EdgeStateSummaries.(KioskExecutionEndpointId, KioskId)` | `KioskExecutionEndpoints.(Id, KioskId)` |

`[Unclear]` whether an equivalent structural guarantee exists for every cross-context reference not listed here — this is an enumerated set, not a universal claim (`srs.md` NFR-023).

### 4.6 True 1:1 relationships

`[Supported]` (`WithOne` + unique FK index): `KioskExecutionEndpoint ↔ ExecutionEndpointMqttCredential`; `KioskExecutionEndpoint ↔ ExecutionEndpointReadinessProjection`; `Kiosk ↔ KioskConnectivityProjection`.

### 4.7 High-volume / time-based indexes

`[Supported]`, per `database_inventory.md` §4 (cross-checked against `docs/data/DATA_MODELING_RULES.md`'s named list, not independently re-opened here):

| Table | Time index found | Verdict |
|---|---|---|
| `KioskHeartbeats` | `(KioskId, ReportedAt)` | present |
| `DeviceEvents` | `(DeviceId, OccurredAt)`, `(KioskId, OccurredAt)` | present |
| `OperationLogs` | `(KioskId, OccurredAt)` | present |
| `SyncEventInbox` | `(SourceNodeId, EventType, OccurredAt)`; retry-scan index `(Status, NextRetryAt, LockedUntil)` | present |
| `SyncDeadLetters` | `(Status, FailedAt)` | present |
| `EdgeCommandDeliveryAttempts` | only unique `(EdgeCommandId, DeliveryAttemptNo)` — **no `SentAt`-bearing index** | `[Unclear]`/gap vs. documented intent |
| `ProductionExecutionRecords` | `(KioskExecutionEndpointId, Status, LastExecutorReportedAt)` — time column only trailing in a composite | partial |
| `ProductionEventCheckpoints`, `EdgeStateSummaries` | none — unique-per-executor upsert tables, not growing logs | `[Unclear]` whether grouping with high-volume tables in `docs/data/DATA_MODELING_RULES.md` is intentional |

No PostgreSQL native table partitioning is configured for any table. `[Supported]`.

---

## 5. JSON Field Physical Mapping

`[Supported]` — every `string` property ending in `"Json"` maps to a `jsonb` column by the global reflection-based convention (§2); this mechanism is directly observed in code. The grouping of those columns into the four roles below, however, is `[Inferred]`: `database_inventory.md` §5 itself describes this taxonomy as cross-checked against `docs/data/JSON_FIELD_RULES.md` and field-naming conventions, not a role tag stated verbatim on each field in code — treat the table below as an interpretive categorization layered on top of the `[Supported]` `jsonb` mapping fact, not as an additional physical-schema fact in its own right.

| Role | Representative fields (entity) |
|---|---|
| Source-of-truth configuration (mutable pre-publish, versioned) | `Store.OpeningHoursJson`, `Kiosk.SettingsJson`, `Recipe.InstructionsJson`, `RobotProgram.ProgramManifestJson`, `ConfigurationRelease.ManifestJson`, `RobotArtifactTechnicalContract.ContractJson`, `IngredientDispenserState.LevelToQuantityProfileJson`, `ProductionPackageVersion.ManifestJson` |
| Immutable order/execution-time snapshot | `OrderItem.RecipeSnapshotJson`, `PaymentTransaction.RawRequestJson`/`RawResponseJson`, `ProductionPackageProductDefinition.ProductSnapshotJson` |
| Append-only external evidence / debug payload | `PaymentCallback.PayloadJson`, `SyncEventInbox.PayloadJson`/`HeadersJson`, `DeviceEvent.PayloadJson`, `EdgeCommand.PayloadJson`, `OperationLog.PayloadJson`, `KioskHeartbeat.PayloadJson`, `IngredientDispenserState.SensorPayloadJson` |
| Metadata (non-critical extension data) | `Organization.MetadataJson`, `Product.MetadataJson`, `Ingredient.MetadataJson`, `Device.MetadataJson` |

`[Unclear]` A few source-of-truth JSON fields lack a paired schema-version column even though most of their siblings have one: `RobotArtifact.MetadataJson`/`RobotArtifactTemplate.MetadataJson`, `ExecutionRoute.RequiredCapabilitiesJson`/`SupportedOptionCodesJson`, `Device.MetadataJson`/`DeviceModel.MetadataJson` (unlike `DeviceModel.CapabilitiesJson`, which is paired). Not confirmed whether intentional (version embedded in the JSON body itself) or a gap. `[Supported]` as an observed asymmetry; `[Unclear]` as to cause — `database_inventory.md` §9 items 8–9.

---

## 6. Physical Deployment and Operational Notes

This section is operational/deployment context (engine configuration, migration history, retention jobs, storage topology) rather than schema-defining fact — it should not be read as an extension of the table/index/constraint definitions in §3–§5.

- **Migration history**: 5 migrations, in order — `InitialCreate` (~68 tables), `CatchUpProductionPackageAndExecutionWorkflows` (+17 tables), `CompleteLocalOperationalWorkflows` (+11 tables), `CompleteLocalOperationalChanges` (+1 table), `AddProductionIncidents` (+2 tables). Total ≈ **99 tables** created cumulatively (a sum of `CreateTable` calls across migration history). `[Unclear]` This is a **cumulative creation count**, not necessarily a lower bound on the current schema: it reflects how many `CreateTable` statements have run over the project's history, and would not account for any table later dropped or renamed by a subsequent migration (none were identified in this evidence pass, but the count was not cross-checked against `IceBotDbContextModelSnapshot.cs` or a live database to confirm no such change occurred). `[Supported]` as a historical creation-event count; `[Unclear]` as an exact current-schema count.
- **Manual migration steps**: at least one migration carries a hand-written raw-SQL pre-flight check (`CompleteLocalOperationalWorkflowsManualSteps.EnsureUniqueProviderPaymentIdentity`) that raises an exception if duplicate `(Provider, ProviderOrderCode)` pairs already exist in `PaymentTransactions` before the corresponding unique index is added — evidence that some constraint additions are treated as data-safety-gated rather than blind schema pushes. `[Supported]`.
- **Soft delete**: `ISoftDeletable` entities get an automatic global EF query filter `DeletedAt IS NULL`, **except** a hard-coded exception list of 12 principal types (`Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, `KioskExecutionEndpoint`), which require an explicit `WhereNotDeleted()` extension call instead. `[Supported]`; `[Inferred]` that every query against these 12 types actually applies the filter where needed was not independently verified (developer-responsibility convention, not an audited guarantee).
- **Advisory locking**: `PostgresAdvisoryLockManager` opens a dedicated connection and calls `pg_try_advisory_lock`/`pg_advisory_unlock` via raw SQL, used for distributed job/singleton-worker coordination outside EF Core's own transaction machinery (e.g., robot-artifact orphan cleanup). `[Supported]`.
- **Data retention**: configurable batched deletes (`DataRetentionOptions`) — defaults `HeartbeatDays=30`, `DeviceEventDays=90`, `OperationLogDays=90`, `ProcessedSyncInboxDays=180`, `ExpiredIdentityCredentialDays=30`, `NotificationDeliveryDays=90`; deletes run in bounded batches (`BatchSize=1000`, `MaxBatchesPerRun=20`) rather than one unbounded `DELETE`. `[Supported]`. `NotificationDeliveryDays` exists in code but is not mentioned in the corresponding documentation's retention list — a minor doc/code gap, not treated as a design defect. `[Unclear]` whether this omission is intentional.
- **Object storage split**: robot artifact binaries (`.lua` files) are **not** stored in PostgreSQL — only metadata (`StorageKey`, `Checksum`, `ContentLengthBytes`) lives in `RobotArtifacts`/`RobotArtifactTemplates`; the bytes live in MinIO, an S3-compatible object store running as a sibling container. `[Supported]`.
- **No native partitioning**: confirmed absent from all 5 migrations; `docs/data/DATA_MODELING_RULES.md`'s partition-key plan (not re-opened here) is documentation of intent, not implemented state. `[Supported]` as an absence; `[Unclear]` timeline for future implementation.

---

## 7. Evidence Notes

- Platform, provider, connection configuration: `database_inventory.md` §7.
- Global EF Core conventions (precision, string length, JSON mapping, key generation, delete behavior, auto-indexing): `database_inventory.md` §4 ("Global index conventions"), §7.
- Soft-delete-aware and unfiltered unique indexes: `database_inventory.md` §4.
- Partial/filtered unique indexes and their exact predicates: `database_inventory.md` §4.
- Check constraints: `database_inventory.md` §4.
- Composite tenant-consistency FKs: `database_inventory.md` §3.
- True 1:1 relationships: `database_inventory.md` §3.
- High-volume/time-based index table, including the `EdgeCommandDeliveryAttempts` gap: `database_inventory.md` §4.
- JSON field roles and schema-version pairing: `database_inventory.md` §5.
- Migration history, manual steps, soft-delete exception list, advisory locking, data retention, object storage split, partitioning absence: `database_inventory.md` §7.
- Cross-checked physical table trimming decisions against `erd.md`, which independently selected a readable subset of the same 98-table model from the same source.

---

## 8. Open Questions

- `[Open Question]` Whether the global `DeleteBehavior.Restrict` convention loop silently overrides the explicitly-configured `Cascade` exceptions was not settled by static code reading alone; resolving this would require either a query against the generated migration SQL/model snapshot's `onDelete` values or a runtime check (`database_inventory.md` §9 item 6; `srs.md` NFR-004). This is the single highest-impact open item for physical design, since it determines actual delete behavior for several parent/child pairs.
- `[Open Question]` `EdgeCommandDeliveryAttempts` is missing its documented `SentAt`-bearing time index despite being named a high-volume table with a `SentAt` partition key in the data-modeling documentation (`database_inventory.md` §9 item 3) — should this index be added, or is the documentation's expectation stale?
- `[Open Question]` `ProductionEventCheckpoints`/`EdgeStateSummaries` are structurally bounded upsert tables (one row per executor or per executor+kind), not growing append-only logs, despite being grouped with genuinely high-volume tables in the data-modeling documentation — worth a reviewer's confirmation that this grouping is intentional rather than a documentation oversight (`database_inventory.md` §9 item 4).
- `[Open Question]` `ExecutionEndpointCredentialBinding.PublicKeyPem` has no `HasMaxLength` override, so it inherits the global 500-character default — unusually short for a PEM-encoded public key. Not resolved whether this is intentional (e.g., only a fingerprint is actually stored for mTLS, and this field serves a narrower alternate-mode use) or an oversight (`database_inventory.md` §9 item 11).
- `[Open Question]` The two different configuration keys for the database connection string (`CONNECTIONSTRING` at runtime vs. `ConnectionStrings:IceBot_DB` at design time) is a real operational divergence; whether this is intentional (e.g., deliberately different deployment pipelines) or should be unified was not determined by this evidence pass (`database_inventory.md` §9 item 10).
- `[Open Question]` `Device.MetadataJson`/`DeviceModel.MetadataJson` lack a schema-version column while `DeviceModel.CapabilitiesJson` has one — and `ExecutionRoute.RequiredCapabilitiesJson`/`SupportedOptionCodesJson` similarly lack a dedicated version column despite the data documentation describing an internal fixed schema version envelope — whether these are consistent design choices (version embedded in the JSON body itself) or gaps was not resolved (`database_inventory.md` §9 items 8–9).
- `[Open Question]` Whether the current physical table count (99, from summed migration `CreateTable` calls) matches an independently-queried current-schema snapshot was not verified in this pass — see §3 for the three distinct counting rules in play (DbSets, named physical tables including `AccountStores`, and cumulative migration creation events) and the fact that none has been reconciled against a live schema or `IceBotDbContextModelSnapshot.cs`.
- `[Open Question]` Are the numeric enum literals embedded in the partial-unique-index predicates (§4.3) documented anywhere as stable, intentionally-numbered contracts, or could a future enum reordering silently change which rows a given filtered index matches? Not resolved by the cited evidence.
- `[Open Question]` Are the hand-written manual migration-step classes (e.g. `CompleteLocalOperationalWorkflowsManualSteps.EnsureUniqueProviderPaymentIdentity`) mandatory, audited deployment steps, or optional/best-effort checks? How and when they are invoked relative to the standard `dotnet ef database update` pipeline was not established by the cited evidence.
- `[Open Question]` What is the required soft-delete visibility/restoration policy (if any) for the 12 principal types excluded from the automatic query filter — can a deleted `Order` or `Account`, for example, ever be viewed or restored by an authorized actor? Not established by the cited evidence beyond the structural mechanism itself (§6).
