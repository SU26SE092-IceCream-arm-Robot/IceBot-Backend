# Database Inventory — IceBot Backend

Source priority per `deliverables/DELIVERABLES_AGENT.md`: Domain entities → Infrastructure EF Core configurations/DbContext/migrations → `docs/data/*`, `docs/architecture/MULTI_TENANCY_RULES.md` → inferred, clearly marked. No files under `src/` or `docs/` were modified to produce this document.

Scope: `src/Domain/**/Entities` (and sibling `Enums`/`ValueObjects`/`Projections`), `src/Infrastructure/Data/**` (DbContext, `Configurations/**`, `Migrations/**`).

---

## 0. How to read this document

- **Proven** = directly observed in a `.cs` file cited next to the claim.
- **Inferred** = reasonable reading of code/naming where no explicit statement exists; marked inline as *(inferred)*.
- Section 9 collects genuine discrepancies found between code and `docs/data/*` / `docs/architecture/MULTI_TENANCY_RULES.md` — these are not defects to silently "fix" in this document, just facts to flag for reviewers.

---

## 1. Entity List

`IceBotDbContext` (`src/Infrastructure/Data/IceBotDbContext.cs:109-214`) exposes ~130 `DbSet<T>` properties. Grouped by bounded context (folder under `src/Domain/`):

### Tenants (`src/Domain/Tenants/Entities`)
| Entity | Table |
|---|---|
| Organization | `Organizations` |
| Store | `Stores` |
| Kiosk | `Kiosks` |
| KioskOperationalStateTransition | `KioskOperationalStateTransitions` |
| FranchiseOnboarding | `FranchiseOnboardings` |

### Identity (`src/Domain/Identity/Entities`)
| Entity | Table |
|---|---|
| Account | `Accounts` |
| AccountRole | `AccountRoles` |
| AccountNotificationDevice | `AccountNotificationDevices` |
| AccountInvitation | `AccountInvitations` |
| PasswordResetRequest | `PasswordResetRequests` |
| RefreshToken | `RefreshTokens` |
| Role | `Roles` |
| — (join table, no domain class) | `AccountStores` (Account↔Store many:many) |

### Catalog (`src/Domain/Catalog/Entities`)
| Entity | Table |
|---|---|
| ProductCategory | `ProductCategories` |
| Product | `Products` |
| ProductVariant | `ProductVariants` |
| OptionGroup | `OptionGroups` |
| ProductOption | `ProductOptions` |
| ProductOptionIngredientRequirement | `ProductOptionIngredientRequirements` |
| Recipe | `Recipes` |
| RecipeItem | `RecipeItems` |
| Ingredient | `Ingredients` |

### Inventory (`src/Domain/Inventory/Entities`)
| Entity | Table |
|---|---|
| IngredientDispenserState | `IngredientDispenserStates` |
| InventoryTopologyChangeRecord | `InventoryTopologyChangeRecords` |
| InventoryTopologyRebindRecord | `InventoryTopologyRebindRecords` |
| StockMovement | `StockMovements` |

### SalesCatalog (`src/Domain/SalesCatalog/Entities`)
| Entity | Table |
|---|---|
| Menu | `Menus` |
| MenuItem | `MenuItems` |
| MenuItemProductOption | `MenuItemProductOptions` |

### Orders (`src/Domain/Orders/Entities`, `src/Domain/Orders/Incidents`)
| Entity | Table |
|---|---|
| Order | `Orders` |
| OrderItem | `OrderItems` |
| OrderItemOption | `OrderItemOptions` |
| OrderItemOptionIngredientRequirement | `OrderItemOptionIngredientRequirements` |
| OrderStatusHistory | `OrderStatusHistories` |
| OrderItemStatusHistory | `OrderItemStatusHistories` |
| ProductionIncident | `ProductionIncidents` |
| ProductionIncidentHistory | `ProductionIncidentHistories` |

### Payments (`src/Domain/Payments/Entities`)
| Entity | Table |
|---|---|
| PaymentMethod | `PaymentMethods` |
| PaymentTransaction | `PaymentTransactions` |
| PaymentCallback | `PaymentCallbacks` |
| Refund | `Refunds` |

### Devices (`src/Domain/Devices/**`)
| Entity | Table |
|---|---|
| DeviceType | `DeviceTypes` |
| DeviceModel | `DeviceModels` |
| Device | `Devices` |
| DeviceEvent | `DeviceEvents` |
| KioskHeartbeat | `KioskHeartbeats` |
| KioskConnectivityProjection | `KioskConnectivityProjections` |
| KioskExecutionEndpoint | `KioskExecutionEndpoints` |
| ExecutionEndpointCredentialBinding | `ExecutionEndpointCredentialBindings` |
| ExecutionEndpointMqttCredential | `ExecutionEndpointMqttCredentials` |
| ExecutionEndpointReadinessProjection | `ExecutionEndpointReadinessProjections` |
| ExecutionEndpointCapabilityProjection | `ExecutionEndpointCapabilityProjections` |
| ExecutionEndpointRequestNonce | `ExecutionEndpointRequestNonces` |
| ExecutionEndpointSupportedRobotTarget | `ExecutionEndpointSupportedRobotTargets` |

### RobotConfiguration (`src/Domain/RobotConfiguration/**`)
| Entity | Table |
|---|---|
| RobotProgram | `RobotPrograms` |
| RobotProgramArtifact | `RobotProgramArtifacts` |
| RobotArtifact | `RobotArtifacts` |
| RobotArtifactTemplate | `RobotArtifactTemplates` |
| RobotArtifactTechnicalContract | `RobotArtifactTechnicalContracts` |
| RobotArtifactDeclaredEffect | `RobotArtifactDeclaredEffects` |
| RobotArtifactOrderingConstraint | `RobotArtifactOrderingConstraints` |
| RobotAuthoringImport | `RobotAuthoringImports` |
| RobotAuthoringImportItem | `RobotAuthoringImportItems` |

### ProductionConfiguration / ProductionExecution / ProductionPackages
| Entity | Table |
|---|---|
| ConfigurationRelease | `ConfigurationReleases` |
| ExecutionRoute | `ExecutionRoutes` |
| ExecutionRouteRobotBinding | `ExecutionRouteRobotBindings` |
| KioskConfigurationDeployment | `KioskConfigurationDeployments` |
| ControllerArtifactSetDeployment | `ControllerArtifactSetDeployments` |
| ControllerArtifactSetItem | `ControllerArtifactSetItems` |
| OrderExecutionRecord | `OrderExecutionRecords` |
| ProductionExecutionRecord | `ProductionExecutionRecords` |
| ProductionPackage | `ProductionPackages` |
| ProductionPackageVersion | `ProductionPackageVersions` |
| ProductionPackageProductDefinition | `ProductionPackageProductDefinitions` |
| ProductionPackageArtifactDefinition | `ProductionPackageArtifactDefinitions` |
| ProductionPackageProgramBlueprint | `ProductionPackageProgramBlueprints` |
| ProductionPackageProgramSlot | `ProductionPackageProgramSlots` |
| ProductionPackageRouteBlueprint | `ProductionPackageRouteBlueprints` |
| ProductionPackageInstallation | `ProductionPackageInstallations` |
| ProductionPackageMaterialization | `ProductionPackageMaterializations` |
| ProductionComposition | `ProductionCompositions` |
| ProductionPackageUpgrade | `ProductionPackageUpgrades` |
| ProductionPackageUpgradeMenuChange | `ProductionPackageUpgradeMenuChanges` |
| ProductionPackageUpgradeMenuOptionChange | `ProductionPackageUpgradeMenuOptionChanges` |
| ProductionPackageUpgradeEndpointTarget | `ProductionPackageUpgradeEndpointTargets` |
| ProductionPackageUpgradeRollbackAttempt | `ProductionPackageUpgradeRollbackAttempts` |
| ProductionPackageUpgradeCatalogIdentityChange | `ProductionPackageUpgradeCatalogIdentityChanges` |
| ProductionPackageUpgradeAvailabilityChange | `ProductionPackageUpgradeAvailabilityChanges` |

### Operations (`src/Domain/Operations/Entities`)
| Entity | Table |
|---|---|
| Alert | `Alerts` |
| MaintenanceTicket | `MaintenanceTickets` |
| OperationLog | `OperationLogs` |
| NotificationDelivery | `NotificationDeliveries` |

### Sync (`src/Domain/Sync/**`)
| Entity | Table |
|---|---|
| SyncEventInbox | `SyncEventInbox` (singular table name) |
| ProductionEventCheckpoint | `ProductionEventCheckpoints` |
| EdgeStateSummary | `EdgeStateSummaries` |
| SyncDeadLetter | `SyncDeadLetters` |
| SyncDeadLetterRetryAttempt | `SyncDeadLetterRetryAttempts` |
| EdgeCommand | `EdgeCommands` |
| EdgeCommandDeliveryAttempt | `EdgeCommandDeliveryAttempts` |

**Evidence**: `src/Infrastructure/Data/IceBotDbContext.cs:109-214`; migration table names confirmed via `migrationBuilder.CreateTable` calls across `src/Infrastructure/Migrations/20260710155643_InitialCreate.cs`, `20260715152003_CatchUpProductionPackageAndExecutionWorkflows.cs`, `20260720011035_CompleteLocalOperationalWorkflows.cs`, `20260721212101_CompleteLocalOperationalChanges.cs`, `20260722183441_AddProductionIncidents.cs`.

---

## 2. Important Attributes (by bounded context)

Only identifying keys, business-meaningful fields, status/lifecycle enums, money/quantity fields, and snapshot fields are listed — full property lists are in the cited entity files.

### Tenants
- **Organization** (`Tenants/Entities/Organization.cs`): `Code`, `Name`, `LegalName`, `TaxCode`, `Status: EntityStatus`, `MetadataJson`.
- **Store** (`Store.cs`): `OrganizationId`, `Code`, `StoreType`, `Status`, `TimeZone`, `Latitude/Longitude`, `OpeningHoursJson` + `OpeningHoursSchemaVersion`, sales-pause fields (`SalesPausedAt/Until`, `SalesPauseReason`, actor ids) mutated only via `PauseSales`/`ResumeSales`.
- **Kiosk** (`Kiosk.cs`): `OrganizationId`, `StoreId`, `Code`, `Status: KioskStatus`, `OperationalState: KioskOperationalState` (private-set, changed via `ChangeOperationalState`), `SerialNumber`, `ConfigurationVersion`, `SettingsJson` + `SettingsSchemaVersion`.
- **FranchiseOnboarding** (`FranchiseOnboarding.cs`): `OrganizationId`, `IdempotencyKey`, `RequestChecksum` (64-char), `RequestJson`, `Status: FranchiseOnboardingStatus`, `StoreId/KioskId/PackageInstallationId` (progressively filled as onboarding advances).

### Identity
- **Account** (`Account.cs`): `UserName`, `Email` (+confirmation flags), `Password: HashedPassword?` (value object → `PasswordHash` column), `Status: AccountStatus`, Google SSO fields (`GoogleSubjectId/Email`), `LockedUntil`, `FailedLoginCount`.
- **AccountRole** (`AccountRole.cs`): `AccountId`, `RoleId` (long), `OrganizationId/StoreId/KioskId` (all nullable — role scope), `IsActive`, `AssignedAt/AssignedByAccountId`.
- **Role** (`Role.cs`, base `CatalogEntity`): `Code`, `Name`, `IsSystemRole`, `Priority`.
- **RefreshToken/PasswordResetRequest/AccountInvitation**: all hold a `TokenHash`, expiry, and usage/revocation timestamps — none store the raw token.
- **AccountNotificationDevice**: `InstallationId`, `Platform`, `PushToken`/`PushTokenHash`, `InvalidatedAt/InvalidationReason`.

### Catalog
- **Product** (`Product.cs`, implements `IKioskScoped`): `OrganizationId/StoreId/KioskId` (nullable), `TemplateProductId` (self-FK lineage), `CategoryId`, `Code`, `BasePrice`, `Currency`, `ScopeType: TenantScopeType` (default `Global`), `MetadataJson`.
- **ProductVariant** (`ProductVariant.cs`): `ProductId`, `Code`, `VariantType`, `FulfillmentType` enum, `SizeCode`, `BasePrice`.
- **OptionGroup**/**ProductOption**: `SelectionType`, `Min/MaxSelections`; `ProductOption.ExecutionImpact: ProductOptionExecutionImpact` (`CommercialOnly` vs `ProductionAffecting`), `PriceDelta`, `TemplateProductOptionId` (lineage field, present but **no FK configured** — see §9).
- **Recipe** (`Recipe.cs`, implements `IKioskScoped`): same scope quartet as Product plus `ProductVariantId`, `TemplateRecipeId`, `Version`, `Status: RecipeStatus` (Draft→Published→Active→Retired), `IsDefault`, `InstructionsJson` + `InstructionsSchemaVersion`.
- **RecipeItem**: `RecipeId`, `IngredientId`, `Quantity`, `Unit`, `StepOrder`, `IsOptional`.
- **Ingredient**: `Code`, `IngredientType`, `Unit`, `IsPerishable`, `IsAllergen`, `MetadataJson`.

### Inventory
- **IngredientDispenserState** (base `SyncAggregateEntity`): `DeviceId`, `KioskId?`, `IngredientId`, `ContainerCode`, `CurrentLevelStatus: IngredientLevelStatus`, `EstimatedQuantity/CapacityQuantity`, `LevelToQuantityProfileJson` + schema version, `SensorPayloadJson`, `IsActive`. Mutated via `ConfigureContainer`/`RecordSensorLevel`/`Refill`/`Consume`/`Retire`/`Reactivate`, each spawning a `StockMovement`.
- **StockMovement** (base `AppendOnlySyncEntity`, implements `IStoreScoped`): `IngredientDispenserStateId`, `OrganizationId/StoreId/KioskId/DeviceId` (all nullable), `SourceEventId`, `MovementType` (free-text: `REFILL`/`CONSUME`/`ADJUST_ESTIMATE`), signed `Quantity`, `BalanceBefore/After`, `OccurredAt`.
- **InventoryTopologyChangeRecord / …RebindRecord**: append-only history rows with before/after snapshots of container state (`IsActive`, `CapacityQuantity`, `Unit`) or of a container-to-container rebind (`EstimateDisposition`, `TransferredQuantity`).

### SalesCatalog / Orders
- **Menu** (`Menu.cs`, implements `IKioskScoped`): `OrganizationId/StoreId/KioskId` (nullable), `Status: MenuStatus`, `ScopeType` (default `Organization` — no global fallback), `Currency`, `EffectiveFrom/To`, `MetadataJson` + schema version.
- **MenuItem**: `MenuId`, `ProductId`, `ProductVariantId`, `RecipeId?`, `DisplayName`, `Price`, `DiscountAmount`, `Status: MenuItemStatus`.
- **Order** (`Order.cs`, implements `IStoreScoped`, most setters `private`): `OrganizationId?`, `StoreId?`, `KioskId` (required), `OrderNumber`, `IdempotencyKey?`, `ClientOrderId?`, `Channel: OrderChannel`, `Status: OrderStatus` (rich state machine — 15 values incl. `RefundRequired`, `FulfillmentIssue`, `Compensated`), `PaymentStatus`, money fields (`SubtotalAmount/DiscountAmount/TaxAmount/TotalAmount/PaidAmount`), `PlacedAt/PaymentDeadlineAt/PaidAt/CompletedAt/CancelledAt`.
- **OrderItem**: carries **immutable order-time snapshots** — `MenuItemCodeSnapshot/NameSnapshot`, `ProductCodeSnapshot/NameSnapshot`, `ProductVariantCodeSnapshot/NameSnapshot`, `RecipeVersionSnapshot`, `UnitPrice` (price at order time), `RecipeSnapshotJson` + `RecipeSnapshotSchemaVersion` (default `2`), `Status: OrderItemStatus`.
- **OrderItemOption**: snapshots `OptionGroupCodeSnapshot`, `CodeSnapshot`, `NameSnapshot`, `UnitPriceDelta`, `ExecutionImpact`.
- **OrderStatusHistory / OrderItemStatusHistory**: append-only transition logs; **inconsistent base class** — `OrderStatusHistory` is a full `BusinessEntity` (audit+soft-delete) while `OrderItemStatusHistory` is a bare `GuidEntity` (no audit fields at all) despite serving the same role (§9).
- **ProductionIncident** (`Orders/Incidents/ProductionIncident.cs`): `OrganizationId?/StoreId?`, `KioskId` (required), `OrderId/OrderItemId`, `SourceCommandId/SourceProductionJobId`, snapshots (`OrderNumberSnapshot`, `ProductNameSnapshot`, …), `Status: ProductionIncidentStatus`, `PhysicalOutputState`, `InspectionOutcome`, `Resolution`; state machine `Open/AwaitingInspection → ResolutionSelected → ResolutionInProgress → Resolved`.

### Payments
- **PaymentMethod** (base `CatalogEntity`, long Id, no soft delete): `Code`, `Provider`, `MethodType`, `IsOnline`.
- **PaymentTransaction** (`PaymentTransaction.cs`): `OrderId`, `PaymentMethodId`, `TransactionNumber` (immutable business key), `IdempotencyKey?`, `Provider`, `ProviderOrderCode/PaymentLinkId/TransactionId`, `Amount/PaidAmount` (decimal), `Status: PaymentTransactionStatus`, `SettlementDisposition: PaymentSettlementDisposition` (private-set — `Unassigned/Primary/DuplicateRefundRequired/DuplicateResolved`), retry fields (`RetryCount/MaxRetries/NextRetryAt`), `RawRequestJson`/`RawResponseJson`.
- **PaymentCallback** (base `AppendOnlyEntity`, no soft delete): `PaymentTransactionId`, `Provider`, `EventType`, `ProviderEventId?`, `PayloadJson`, `Signature`, `ProcessingStatus`, `ProcessingAttempts/MaxProcessingAttempts`.
- **Refund** (`Refund.cs`): `PaymentTransactionId`, `RefundNumber` (immutable business key), `IdempotencyKey?`, `ProviderRefundId?`, `Amount`, `Status: RefundStatus`, retry fields.

### Devices
- **Device** (`Device.cs`, most setters `private`): `DeviceTypeId`, `DeviceModelId?`, `KioskId?`, `Code`, `Status: DeviceStatus` (state machine via `CanTransitionTo`), `SerialNumber?`, `FirmwareVersion`, `MetadataJson` (no schema-version pairing found).
- **DeviceModel**: `DeviceTypeId`, `CapabilitiesJson` + `CapabilitiesSchemaVersion` (versioned pair), `MetadataJson` (unversioned).
- **DeviceEvent** (base `AppendOnlySyncEntity`): `DeviceId`, `KioskId?`, `EventId` (business/idempotency id), `EventType`, `Severity`, `PayloadJson`, `OccurredAt`.
- **KioskHeartbeat** (base `AppendOnlySyncEntity`): `KioskId`, `NodeId`, `HeartbeatSequence?`, `ReportedAt`, `Status: KioskHeartbeatStatus`, CPU/Memory/Disk usage percentages (`decimal`), `PayloadJson`.
- **KioskExecutionEndpoint** (`KioskExecutionEndpoint.cs`, most setters `private`): `KioskId`, `EndpointCode`, `ExecutionProfile: KioskExecutionProfile` (`FullEdge`/`LowCostController`), `AuthenticationMode`, mutually-exclusive `FullEdgeRuntimeId` vs `ControllerId` (enforced by DB check constraint, §4), plus two parallel "active configuration" snapshot blocks (Full-Edge deployment vs low-cost controller artifact-set).
- **ExecutionEndpointMqttCredential**: `Username` (= endpoint id), `BrokerProvider`, `CredentialVersion` (incremented per rotation), `Status: ExecutionEndpointMqttCredentialStatus` (7-state rotation/revoke machine).
- **ExecutionEndpointReadinessProjection / …CapabilityProjection**: read-model rows updated via monotonic `StateRevision` guard (`Apply` rejects `revision <= StateRevision`); `Readiness/Activity/Safety` tri-state enums.

### RobotConfiguration
- **RobotProgram** (`RobotProgram.cs`, implements `IKioskScoped`): `OrganizationId/StoreId/KioskId/DeviceId` (nullable), `ScopeType: TenantScopeType`, `Status: RobotProgramStatus`, `ProgramManifestJson` + `ProgramManifestSchemaVersion` (1 or 2), `ProgramManifestChecksum`. `ValidateScope()` rejects `ScopeType.Global` at creation despite the enum defining it (§9). Interface setters for `OrganizationId/StoreId/KioskId` throw after draft creation — scope is immutable once set.
- **RobotProgramArtifact** (base `RobotConfigurationEntity`, i.e. sync-enabled): `RobotProgramId`, `RobotArtifactId`, `RunOrder` (>0), `ParametersJson` + `ParametersSchemaVersion`, `RequiredOptionCode`.
- **RobotArtifact** (base `RobotConfigurationEntity`): `OrganizationId` (required, non-nullable — single-org-scoped, no Store/Kiosk/Device scoping), `SourceRobotArtifactTemplateId?`, `ArtifactCode`, `Checksum` (SHA-256, validated format), `Status: RobotArtifactStatus`, `TechnicalContractId?`.
- **RobotArtifactTemplate**: same shape as `RobotArtifact` but global (no `OrganizationId`) and not sync-enabled (`BusinessEntity`, not `RobotConfigurationEntity`).
- **RobotArtifactTechnicalContract**: `OrganizationId?` (null = global), `ContractCode/ContractVersion`, `SchemaVersion` (1 or 2), `Status: RobotArtifactContractStatus`, `ContractJson` (built at `Publish()`), `ContractChecksum`. Child entities `RobotArtifactDeclaredEffect` (effect taxonomy: System/Ingredient/Option/Motion/Composite) and `RobotArtifactOrderingConstraint` (Phase/BeforeEffect/AfterEffect) are declared in the same source file.
- **RobotAuthoringImport**: `OrganizationId` (required) + optional `StoreId/KioskId/DeviceId`, `ClientExportId`, `ImportChecksum`, `IdempotencyKey`, `Status: RobotAuthoringImportStatus`, `ValidationReportJson`, `ComposedOptionCodesJson`. Child `RobotAuthoringImportItem`: `ArtifactCode`, `RunOrder`, `LuaChecksum/SidecarChecksum`, `Status: RobotAuthoringImportItemStatus`.

### ProductionConfiguration / ProductionExecution / ProductionPackages
- **ConfigurationRelease** (`ConfigurationRelease.cs`): `OrganizationId` (required), `ReleaseNumber` (long), `Status: ConfigurationReleaseStatus`, `ManifestJson` + `ReleaseManifestSchemaVersion`, `ReleaseChecksum`. *(No Store/Kiosk/Device/ProductVariant/Recipe/RobotProgram columns exist directly on this entity — those scopes live on the related `ExecutionRoute`/`Recipe`/`RobotProgram`, see §9.)*
- **ExecutionRoute**: `ConfigurationReleaseId`, `ProductVariantId`, `RecipeId`, `RouteCode`, `Priority`, `RequiredCapabilitiesJson`, `SupportedOptionCodesJson` (default `'[]'`), `ProductionDefinitionJson` + `ProductionDefinitionSchemaVersion`.
- **KioskConfigurationDeployment** (Full-Edge path): `KioskId/OrganizationId/KioskExecutionEndpointId/EdgeRuntimeId/ConfigurationReleaseId`, `IdempotencyKey`, `AttemptNo`, `Status: KioskConfigurationDeploymentStatus`, `WarningCodesJson`.
- **ControllerArtifactSetDeployment/Item** (low-cost controller path): mirrors the above with `ControllerId`, `ActiveSetVersion/Checksum`, per-item `RobotArtifactId`, `ArtifactChecksum`, `ParametersJson`.
- **OrderExecutionRecord / ProductionExecutionRecord** (base `AuditedEntity`, typed projections — not raw executor payload passthroughs): `SourceCommandId`, `KioskExecutionEndpointId`, `Status: ProductionExecutionStatus`, idempotency via `(SourceExecutorId, LastAppliedSourceEventId)` + monotonic `LastAppliedSequenceNumber`; `ProductionExecutionRecord` additionally carries `PhysicalOutputState` (irreversible Yes-state guard) and `OrderItemId/ProductionUnitNo`.
- **ProductionPackage family** (Package → Version → {ProductDefinition, ArtifactDefinition, ProgramBlueprint→Slot, RouteBlueprint} → Installation → Materialization → Composition; plus a parallel Upgrade family: Upgrade → {MenuChange→MenuOptionChange, EndpointTarget→RollbackAttempt, CatalogIdentityChange, AvailabilityChange}): every "definition"/"snapshot" child stores a `*Json` payload plus a `*Checksum`, and installation/upgrade rows carry `OrganizationId` (+ optional `StoreId/KioskId`) and an `IdempotencyKey`.

### Operations / Sync
- **Alert** (base `SyncAggregateEntity`): `KioskId`, `DeviceId?`, `AlertCode`, `CorrelationKey` (normalized uppercase, private-set), `Severity`, `Status: AlertStatus`, `OccurrenceCount`/`LastOccurredAt` (dedup counters), `SourceType/SourceId` (polymorphic evidence link).
- **MaintenanceTicket** (base `SyncAggregateEntity`, implements `IKioskScoped` explicitly — direct `OrganizationId/StoreId/KioskId`, all required): `TicketNumber`, `IssueCode`, `Priority: MaintenancePriority`, `Status: MaintenanceTicketStatus`, `OperationalImpact: MaintenanceOperationalImpact` (`None`/`BlocksNewOrders`/`RequestsEmergencyStop`).
- **NotificationDelivery** (base `GuidEntity, IAuditable` directly — not `AuditedEntity`): `OrganizationId` (required) + optional `StoreId/KioskId`, `DeliveryKey`, `NotificationType`, `RecipientAccountId`, `DataJson` (default `"{}"`), `Status: NotificationDeliveryStatus`, retry fields.
- **OperationLog** (base `AppendOnlySyncEntity`): `AccountId/KioskId/DeviceId/OrderId` (all optional evidence links), `Action`, `Category`, `Severity`, `PayloadJson`, `OccurredAt`.
- **SyncEventInbox** (base bare `GuidEntity` — no audit fields): `EventId`, `KioskId?`, `SourceNodeId`, `SequenceNumber?`, `PayloadJson`/`HeadersJson`, `Status: SyncEventStatus` (6-state incl. `DeadLettered`), retry/lock fields (`LockId/LockedUntil/NextRetryAt`).
- **SyncDeadLetter** (base bare `GuidEntity`): `SyncEventInboxId?`, `EventId?`, `KioskId?`, `PayloadJson`, `ErrorMessage`, `Status: SyncDeadLetterStatus`, `FailedAt`. Child `SyncDeadLetterRetryAttempt`: `AttemptNumber`, `RequestedByAccountId`, `Succeeded?`.
- **EdgeCommand** (base `AuditedEntity`): `CommandType: EdgeCommandType` (`ExecuteOrder`/`DeployConfiguration`), `KioskId`, `TargetExecutionEndpointId`, `PayloadJson`, `CommandExpiryAt`, `Status: EdgeCommandStatus`. Child `EdgeCommandDeliveryAttempt`: `DeliveryAttemptNo`, `SentAt`, `Outcome: EdgeCommandDeliveryOutcome`.
- **EdgeStateSummary / ProductionEventCheckpoint**: both are **bounded, upsert-style** rows (unique per executor, or per `(executor, kind)`) despite being named in `docs/data/DATA_MODELING_RULES.md`'s high-volume list — see §9.

**Evidence**: individual entity files cited inline above; base classes in `src/Domain/Common/EntityBase.cs`, `BusinessEntity.cs`, `CatalogEntity.cs`, `RobotConfigurationEntity.cs`, `SyncAggregateEntity.cs`, `EntityAbstractions.cs`.

---

## 3. Relationships

### Global relationship convention
`IceBotDbContext.ConfigureEntityConventions` (`IceBotDbContext.cs:269-273`) force-sets **every** foreign key's `DeleteBehavior` to `Restrict` after all `IEntityTypeConfiguration<T>` classes have run — matching `docs/data/DATA_MODELING_RULES.md`'s "Default delete behavior should stay restrictive" rule. A few configuration files explicitly write `DeleteBehavior.Cascade` for true parent-owns-child aggregates (e.g. `RobotArtifactTechnicalContract → RobotArtifactDeclaredEffect/OrderingConstraint`, `RobotAuthoringImport → RobotAuthoringImportItem`, `ProductionPackage* parent → child`, `ControllerArtifactSetDeployment → Item`) — whether the later global loop silently reverts these to `Restrict` is **not confirmed from static reading alone** (flagged in §9; would need a migration snapshot diff or runtime check to settle).

### Notable relationship shapes (beyond plain many:1 FK + `Restrict`)

- **Composite FKs used as a *tenant-consistency* guard** (per `docs/data/DATA_MODELING_RULES.md`'s "Tenant Scope Consistency" section), e.g.:
  - `DeviceEvent → Device` via `(DeviceId, KioskId) → Device(Id, KioskId)` (`DeviceCatalogConfigurations.cs` unique `(Id, KioskId)` + `DeviceTelemetryConfigurations.cs:48-51`).
  - `ExecutionEndpointSupportedRobotTarget → KioskExecutionEndpoint` and `→ Device`, both via `(*, KioskId)` composite pairs (`ExecutionEndpointConfigurations.cs:45-50,147-150`).
  - `EdgeCommand → KioskExecutionEndpoint` via `(TargetExecutionEndpointId, KioskId) → (Id, KioskId)` (`EdgeCommandConfigurations.cs`).
  - `OrderExecutionRecord`/`ProductionExecutionRecord → EdgeCommand` via `(SourceCommandId, KioskExecutionEndpointId) → (Id, TargetExecutionEndpointId)` (`ProductionExecutionConfigurations.cs`).
  - `KioskConfigurationDeployment`/`ControllerArtifactSetDeployment → ConfigurationRelease` via `(ConfigurationReleaseId, OrganizationId) → (Id, OrganizationId)`, and `→ Kiosk` via `(KioskId, OrganizationId)` (`DeploymentConfigurations.cs`).
  - `NotificationDelivery → Store`/`→ Kiosk` via `(StoreId, OrganizationId)`/`(KioskId, OrganizationId)` composite, nullable-safe cast (`OperationConfigurations.cs:79-84`).
  - `ProductionEventCheckpoint`/`EdgeStateSummary → KioskExecutionEndpoint` via `(KioskExecutionEndpointId, KioskId) → (Id, KioskId)` (`SyncIngestionConfigurations.cs`).
  These are exactly the composite-FK tenant-consistency pattern the data-modeling doc calls for (`(ExecutionEndpointId, KioskId)`, `(KioskId, OrganizationId)`, etc.) — confirmed present in code, not merely documented.
- **True 1:1 relationships** (`WithOne` + unique FK index): `KioskExecutionEndpoint ↔ ExecutionEndpointMqttCredential`, `KioskExecutionEndpoint ↔ ExecutionEndpointReadinessProjection`, `Kiosk ↔ KioskConnectivityProjection`.
- **Self-referencing FKs** (template/lineage pattern): `Product.TemplateProductId → Product`, `Recipe.TemplateRecipeId → Recipe`, `RobotArtifactTechnicalContract.SourceContractId → RobotArtifactTechnicalContract`, `RefreshToken.ReplacedByTokenId → RefreshToken` (rotation chain).
- **Many:many via explicit join entity** (no shorthand skip-navigation): `Account ↔ Store` through table `AccountStores`, composite PK `(AccountId, StoreId)`.
- **Unowned/shadow FKs with no navigation property** on the dependent side appear throughout append-only/history tables (e.g. `OrderItemStatusHistory → OrderItem/Account`, `ProductionIncident → Order/OrderItem`, most `Production*Upgrade*` evidence rows) — configured via `HasOne<T>()` rather than `HasOne(x => x.Nav)`.
- **Relationships present as bare Guid columns with no FK constraint at all** (intentional for pure audit/evidence rows, or possibly an oversight — see §9): `InventoryTopologyChangeRecord`'s four reference columns; `InventoryTopologyRebindRecord`'s `Source/ReplacementDeviceId` and `Source/ReplacementIngredientId`; `ProductOption.TemplateProductOptionId`; `OrderItemOption.ProductOptionId`; `OrderItemOptionIngredientRequirement.IngredientId`; several `ProductionIncident` reference ids (`SourceCommandId`, `SourceProductionJobId`, `OpenedByAccountId`, `InspectedByAccountId`, `ResolvedByAccountId`, `RelatedEdgeCommandId`, `RelatedRefundId`).

**Evidence**: `src/Infrastructure/Data/IceBotDbContext.cs:269-273`; per-context `Configurations/*.cs` files cited above.

---

## 4. Constraints / Indexes

### Global index conventions (`IceBotDbContext.ConfigureEntityConventions`, lines 230-267)
- Any entity implementing `IRobotSyncEntity` (i.e. `AppendOnlySyncEntity`/`SyncAggregateEntity`/`RobotConfigurationEntity` subclasses) automatically gets a composite index on `(OriginNodeId, Version)` — applies to `DeviceEvent`, `KioskHeartbeat`, `StockMovement`, `Alert`, `MaintenanceTicket`, `OperationLog`, `RobotArtifact`, `RobotProgram`, `RobotProgramArtifact`, `IngredientDispenserState`. Does **not** apply to `RobotArtifactTemplate`, `RobotArtifactTechnicalContract`, `RobotAuthoringImport(Item)` (all plain `BusinessEntity`, not sync-enabled).
- Any entity implementing `IOrganizationScoped` automatically gets a plain index on `OrganizationId`.
- `GuidEntity.Id` is `ValueGeneratedNever()` (app-generated GUIDs); `LongEntity.Id` is `ValueGeneratedOnAdd()` (DB identity/sequence).

### Soft-delete-aware uniqueness
`EfModelConfigurationConstants` (`src/Infrastructure/Data/Configurations/EfModelConfigurationConstants.cs`) defines `ActiveRowFilter = "\"DeletedAt\" IS NULL"` and `NotNullAndActive(col)`. Reusable business codes are made unique **only among non-deleted rows** using these filters — verified present for `Organization.Code`, `Store.(OrganizationId,Code)`, `Kiosk.(OrganizationId,Code)` / `SerialNumber`, `Account.UserName/Email/GoogleSubjectId`, `Product.(scope…,Code)`, `Recipe.(scope…,ProductVariantId,Code,Version)`, `Menu.(scope…,Code)`, `MenuItem.(MenuId,Code)`, `RobotArtifact.(OrganizationId,ArtifactCode,Checksum)`, `RobotProgram.(scope…,Code)`, `Device.(KioskId,Code)` / `SerialNumber`, `MaintenanceTicket.TicketNumber` — matching the exact list called out in `docs/data/DATA_MODELING_RULES.md`.

Immutable evidence/retry keys are deliberately **not** filtered by `DeletedAt` (per the same doc's rule): `PaymentTransaction.TransactionNumber` (unfiltered unique), `Refund.RefundNumber` (unfiltered unique — even though `Refund` itself *does* get the automatic soft-delete query filter for reads), `Order.OrderNumber` (unfiltered unique), `FranchiseOnboarding.RequestChecksum`/`IdempotencyKey` (filtered by active only for the composite idempotency key, not the checksum alone).

### Partial/filtered unique indexes enforcing a business invariant (not just soft-delete)
- `ProductOption`: unique on `OptionGroupId` filtered `"IsDefault" = TRUE AND "DeletedAt" IS NULL` — at most one default option per group.
- `Recipe`: unique on `ProductVariantId` filtered `"IsDefault" = TRUE AND "Status" <> 4 AND "DeletedAt" IS NULL` — at most one default, non-retired recipe per variant.
- `PaymentTransaction`: unique on `OrderId` filtered `"SettlementDisposition" = 1` — at most one *Primary*-settlement transaction per order.
- `KioskConfigurationDeployment`: unique on `KioskId` filtered `"Status" IN (1, 2)` — at most one Pending/Installed deployment per kiosk.
- `ProductionPackageUpgrade`: unique on `(OrganizationId, SourceInstallationId)` filtered `"DeletedAt" IS NULL AND "Status" IN (0,1,2,3)` — at most one active (non-terminal) upgrade per installation.
- `IngredientDispenserState`: unique on `(DeviceId, ContainerCode)` filtered `"IsActive" = TRUE AND "DeletedAt" IS NULL` — one active container binding per device slot.

### Check constraints (rare — most invariants are enforced in domain code, not the DB)
- `KioskExecutionEndpoint`: `CK_KioskExecutionEndpoints_ProfileIdentity` — `FullEdge` profile requires `ControllerId IS NULL`, `LowCostController` requires `FullEdgeRuntimeId IS NULL`, and an `Active` endpoint must have its profile-matching identity id populated (`ExecutionEndpointConfigurations.cs`).
- `ProductionPackageInstallation`: `CK_ProductionPackageInstallations_KioskRequiresStore` — `KioskId IS NULL OR StoreId IS NOT NULL`.

### High-volume / time-based indexes (per `docs/data/DATA_MODELING_RULES.md`'s named list)
| Table | Time index found in code | Verdict |
|---|---|---|
| `KioskHeartbeats` | `(KioskId, ReportedAt)` | present |
| `DeviceEvents` | `(DeviceId, OccurredAt)`, `(KioskId, OccurredAt)` | present |
| `OperationLogs` | `(KioskId, OccurredAt)` | present |
| `SyncEventInbox` | `(SourceNodeId, EventType, OccurredAt)`; **retry-scan index** `(Status, NextRetryAt, LockedUntil)` present exactly as documented | present |
| `SyncDeadLetters` | `(Status, FailedAt)` | present |
| `EdgeCommandDeliveryAttempts` | only `(EdgeCommandId, DeliveryAttemptNo)` unique — **no `SentAt`-bearing index** | **gap vs. doc** (see §9) |
| `ProductionExecutionRecords` | `(KioskExecutionEndpointId, Status, LastExecutorReportedAt)` — time column only ever trailing in composites, no dedicated `LastExecutorReportedAt` index | partial |
| `ProductionEventCheckpoints`, `EdgeStateSummaries` | none — both are unique-per-executor upsert tables, not growing logs | see §9 |

No PostgreSQL native table partitioning is configured anywhere in `src/Infrastructure/Migrations/**` for any of the above tables — `docs/data/DATA_MODELING_RULES.md`'s partition plan is documentation of *intent*, not yet implemented.

**Evidence**: `src/Infrastructure/Data/IceBotDbContext.cs:230-273`; `src/Infrastructure/Data/Configurations/EfModelConfigurationConstants.cs`; per-context `Configurations/*.cs` files cited above; `docs/data/DATA_MODELING_RULES.md`.

---

## 5. JSON Fields and Why They Exist

`IceBotDbContext.ConfigureEntityConventions` (`IceBotDbContext.cs:234-240`) maps **every** `string` property whose name ends in `"Json"` to a PostgreSQL `jsonb` column via reflection — a blanket rule, not a per-field opt-in. `docs/data/JSON_FIELD_RULES.md` groups these fields into four roles; this section maps that taxonomy onto the fields actually found in code.

### Role 1 — Source-of-truth configuration (mutable pre-publish, versioned)
| Field | Schema-version pair | Entity |
|---|---|---|
| `Store.OpeningHoursJson` | `OpeningHoursSchemaVersion` | Store |
| `Kiosk.SettingsJson` | `SettingsSchemaVersion` | Kiosk |
| `Recipe.InstructionsJson` | `InstructionsSchemaVersion` | Recipe |
| `RobotProgramArtifact.ParametersJson` | `ParametersSchemaVersion` | RobotProgramArtifact |
| `RobotArtifact.MetadataJson` / `RobotArtifactTemplate.MetadataJson` | *(no version field found)* | RobotArtifact(Template) |
| `ConfigurationRelease.ManifestJson` | `ReleaseManifestSchemaVersion` | ConfigurationRelease |
| `RobotArtifactTechnicalContract.ContractJson` | `SchemaVersion` (top-level entity field, 1 or 2) | RobotArtifactTechnicalContract |
| `RobotProgram.ProgramManifestJson` | `ProgramManifestSchemaVersion` (1 or 2) | RobotProgram |
| `ExecutionRoute.RequiredCapabilitiesJson`, `SupportedOptionCodesJson` | *(no dedicated version column on the entity — see §9)* | ExecutionRoute |
| `IngredientDispenserState.LevelToQuantityProfileJson` | `LevelToQuantityProfileSchemaVersion` | IngredientDispenserState |
| `ProductionPackageVersion.ManifestJson` | `ManifestSchemaVersion` | ProductionPackageVersion |
| `ProductionComposition.InputJson` | `InputSchemaVersion` | ProductionComposition |
| `EdgeStateSummary.PayloadJson` | `SummarySchemaVersion` (explicit int, required) | EdgeStateSummary |

### Role 2 — Immutable order/execution-time snapshot
| Field | Entity |
|---|---|
| `OrderItem.RecipeSnapshotJson` (+ `RecipeSnapshotSchemaVersion`) | OrderItem |
| `PaymentTransaction.RawRequestJson` / `RawResponseJson` | PaymentTransaction |
| `ProductionPackageProductDefinition.ProductSnapshotJson` | ProductionPackageProductDefinition |

Product-option selections at order time are stored as **typed** `OrderItemOption` snapshot columns (`OptionGroupCodeSnapshot`, `CodeSnapshot`, `NameSnapshot`, `UnitPriceDelta`), *not* JSON — matching the doc's rule that anonymous checkout/edge execution payloads must not carry arbitrary option JSON.

### Role 3 — Append-only external evidence / debug payload
| Field | Entity |
|---|---|
| `PaymentCallback.PayloadJson` | PaymentCallback |
| `SyncEventInbox.PayloadJson`, `HeadersJson` | SyncEventInbox |
| `SyncDeadLetter.PayloadJson` | SyncDeadLetter |
| `DeviceEvent.PayloadJson` | DeviceEvent |
| `EdgeCommand.PayloadJson` | EdgeCommand |
| `OperationLog.PayloadJson` | OperationLog |
| `KioskHeartbeat.PayloadJson` | KioskHeartbeat |
| `IngredientDispenserState.SensorPayloadJson` | IngredientDispenserState |
| `NotificationDelivery.DataJson` (default `"{}"`) | NotificationDelivery |
| `KioskConfigurationDeployment.WarningCodesJson`, `ControllerArtifactSetDeployment.WarningCodesJson` | deployment risk warnings |
| `RobotAuthoringImport.ValidationReportJson`, `ComposedOptionCodesJson` | RobotAuthoringImport |
| `ProductionComposition.ReportJson` | ProductionComposition |

### Role 4 — Metadata (non-critical extension data)
| Field | Entity |
|---|---|
| `Organization.MetadataJson` | Organization |
| `Product.MetadataJson`, `ProductVariant.MetadataJson`, `ProductOption.MetadataJson`, `MenuItem.MetadataJson` (+`MenuMetadataSchemaVersion`-style pairing on `Menu`/`MenuItem`), `Menu.MetadataJson` | Catalog/SalesCatalog |
| `Ingredient.MetadataJson` | Ingredient |
| `Device.MetadataJson`, `DeviceModel.MetadataJson` (`DeviceModel` also has the *versioned* `CapabilitiesJson`, distinct from its own unversioned `MetadataJson`) | Devices |

Field-name convention observed in code matches the doc: `*ConfigJson`/`*SettingsJson`/`*ParametersJson`/`*InstructionsJson`/`*ProfileJson`/`*ManifestJson` → source-of-truth; `*SnapshotJson` → immutable snapshot; `PayloadJson`/`HeadersJson`/`Raw*Json` → evidence; `MetadataJson` → extension only.

**Evidence**: `src/Infrastructure/Data/IceBotDbContext.cs:234-240`; entity files cited in §2; `docs/data/JSON_FIELD_RULES.md` (role taxonomy, cross-checked against code — not assumed).

---

## 6. Multi-Tenancy Fields

Tenant root is `Organization`; `Store.OrganizationId` and `Kiosk.OrganizationId`/`Kiosk.StoreId` are all **non-nullable** (`Store.cs:8`, `Kiosk.cs:9,17`) — every Store/Kiosk is bound to a tenant at the type level. `TenantScopeType` enum (`src/Domain/Tenants/Enums/TenantScopeType.cs`): `Global=1, Organization=2, Store=3, Kiosk=4, Device=5`.

### Entities implementing the full override hierarchy (`ScopeType` + nullable `OrganizationId`/`StoreId`/`KioskId`[/`DeviceId`] + a `Template*Id` lineage FK)
| Entity | Scope fields | Lineage field | Notes |
|---|---|---|---|
| Product | `IKioskScoped`: Org/Store/Kiosk, all nullable | `TemplateProductId` (self-FK) | `ScopeType` default `Global` |
| Recipe | same as Product, + `ProductVariantId` | `TemplateRecipeId` (self-FK) | `ScopeType` default `Global` |
| Menu | `IKioskScoped` | *(none — no global fallback; must belong to an org)* | `ScopeType` default `Organization` |
| RobotProgram | `IKioskScoped` + `DeviceId` | *(no `TemplateProgramId` field exists in code — doc/context mismatch, §9)* | `ScopeType` default `Organization`; `Global` scope rejected at creation |

Resolution order **Device > Kiosk > Store > Organization > Global** is asserted by `docs/architecture/MULTI_TENANCY_RULES.md` and is consistent with the nullable-narrowing pattern above, but the actual resolution/lookup query logic lives in an application-layer handler that was outside this inventory's `src/Domain` + `src/Infrastructure/Data` scope — *(inferred, not independently re-verified here)*.

### Entities with a single required `OrganizationId` (no override hierarchy — org-owned, not template/global)
`ConfigurationRelease`, `RobotArtifact` (required, non-nullable — no Store/Kiosk/Device scoping unlike RobotProgram), `RobotAuthoringImport` (required + optional Store/Kiosk/Device), `MaintenanceTicket` (required Org **and** Store **and** Kiosk, via explicit `IKioskScoped` fields, not derived by join), `NotificationDelivery` (required Org + optional Store/Kiosk enforced via composite FK), `ProductionPackageInstallation`/`ProductionComposition`/`ProductionPackageUpgrade` (required Org + optional Store/Kiosk where relevant), `FranchiseOnboarding`, `RobotArtifactTechnicalContract` (nullable — `null` = global/shared contract).

### Entities that derive tenant ownership only by joining through a scoped owner (no duplicated `OrganizationId` column)
Per `docs/architecture/MULTI_TENANCY_RULES.md`'s "Operational Entities" list, confirmed in code: `OrderExecutionRecord`, `Alert` (only `KioskId`), `OperationLog` (only `KioskId`/`DeviceId`/`OrderId`), `KioskHeartbeat`/`DeviceEvent` (only `KioskId`/`DeviceId`), `SyncEventInbox`/`SyncDeadLetter` (only `KioskId`, nullable). `Order` itself carries `OrganizationId`/`StoreId` as **nullable** convenience columns alongside a **required** `KioskId` — i.e. Order does duplicate `OrganizationId` (per the doc's "Already applied" list) but as nullable, not required.

### Tenant-scope enforcement mechanism (current v1, per code + `docs/architecture/MULTI_TENANCY_RULES.md`)
No global EF Core tenant query filter is applied anywhere in `IceBotDbContext` — the only automatic global query filters are the soft-delete filter (§7) and the `(OriginNodeId, Version)`/`OrganizationId` auto-indexes (§4), which are indexing conventions, not row-filtering tenant scope. Tenant scoping is enforced explicitly in application-layer handlers/stores (outside this inventory's scope), plus the composite-FK "tenant consistency" pairs documented in §3/§4 (e.g. `(KioskId, OrganizationId)`) that make cross-tenant rows impossible to *persist*, even though normal reads are not filtered by tenant automatically.

**Evidence**: `src/Domain/Tenants/Entities/*.cs`, `src/Domain/Common/EntityAbstractions.cs` (`IOrganizationScoped`/`IStoreScoped`/`IKioskScoped`), `src/Domain/Tenants/Enums/TenantScopeType.cs`, entity files cited in §2, `docs/architecture/MULTI_TENANCY_RULES.md`.

---

## 7. Physical Database Notes

- **Engine**: PostgreSQL 17 (`docker/docker-compose.yml`: `image: postgres:17`, database name `IceBotDB`).
- **Driver/provider**: Npgsql EF Core provider — `UseNpgsql(cs, ...)` in both the runtime DI registration (`src/Infrastructure/DependencyInjection.cs:85`) and the design-time factory (`src/Infrastructure/AppDbContextFactory.cs:31-35`, which also enables `EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)`).
- **Connection string**: read from configuration key `CONNECTIONSTRING` at runtime (`DependencyInjection.cs:76`) or `ConnectionStrings:IceBot_DB` / env var `ConnectionStrings__IceBot_DB` at design time (`AppDbContextFactory.cs:22-23`) — i.e. the two paths use **different configuration keys**, worth flagging for anyone debugging a "works in migrations but not at runtime" connection issue (not further investigated here).
- **Migration history**: 5 migrations as of this inventory, in order: `InitialCreate` (2026-07-10, ~68 tables), `CatchUpProductionPackageAndExecutionWorkflows` (2026-07-15, +17 tables — ProductionPackage family, RobotArtifact contract children), `CompleteLocalOperationalWorkflows` (2026-07-20, +11 tables — FranchiseOnboarding, NotificationDeliveries, ProductionPackageUpgrade family, RobotAuthoringImport(Item)), `CompleteLocalOperationalChanges` (2026-07-21, +1 table — KioskOperationalStateTransitions), `AddProductionIncidents` (2026-07-22, +2 tables — ProductionIncident(History)). Total ≈ **99 tables** created across migration history (source: `migrationBuilder.CreateTable` call count per file). Two migrations also carry hand-written raw-SQL "manual steps" classes (`src/Infrastructure/Migrations/ManualSteps/*ManualSteps.cs`) — e.g. `CompleteLocalOperationalWorkflowsManualSteps.EnsureUniqueProviderPaymentIdentity` runs a pre-flight `DO $$ ... RAISE EXCEPTION` check for duplicate `(Provider, ProviderOrderCode)` pairs in `PaymentTransactions` before the corresponding unique index can be added — evidence that this repo treats certain constraint additions as data-safety-gated, not blind schema pushes.
- **Global EF conventions** (`IceBotDbContext.ConfigureConventions` and `ConfigureEntityConventions`, lines 216-273): `decimal` → `precision(18,4)` for every decimal property unless explicitly overridden; `string` → `maxlength(500)` default; every `string` property ending in `"Json"` → PostgreSQL `jsonb` column type (§5); `GuidEntity.Id` is `ValueGeneratedNever()` (application generates GUIDs, not the DB); `LongEntity.Id` is `ValueGeneratedOnAdd()` (DB-generated identity/sequence — used by `CatalogEntity`-derived tables like `ProductCategories`, `Roles`, `PaymentMethods`, `OptionGroups`, `DeviceTypes`).
- **Soft delete**: `ISoftDeletable` entities get an automatic EF global query filter `DeletedAt IS NULL` **except** a hard-coded exception list in `IceBotDbContext.cs:50-64` — `Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, `KioskExecutionEndpoint`. These 12 principal types have required, non-soft-deleted evidence dependents, so filtering them globally would either hide those dependents or trigger EF's required-navigation warning; normal operational stores for these types must call an explicit `WhereNotDeleted()` extension (`src/Infrastructure/Data/SoftDeleteQueryExtensions.cs`) instead.
- **Advisory locking**: `PostgresAdvisoryLockManager` (`src/Infrastructure/Concurrency/PostgresAdvisoryLockManager.cs`) opens a dedicated `NpgsqlConnection` and calls `pg_try_advisory_lock`/`pg_advisory_unlock` directly via raw SQL — used for distributed job/singleton-worker coordination outside of EF Core's transaction/change-tracking machinery.
- **Object storage split**: Robot artifact binary files (`.lua` scripts) are **not** stored in PostgreSQL — only their metadata (`StorageKey`, `Checksum`, `ContentLengthBytes`) lives in `RobotArtifacts`/`RobotArtifactTemplates`. The actual bytes live in MinIO (`docker/docker-compose.yml` `minio` service; `src/Infrastructure/RobotConfiguration/Storage/ObjectStorage/MinioArtifactObjectStorage.cs`), an S3-compatible object store running as a sibling container.
- **Data retention** (`src/Infrastructure/Persistence/Jobs/DataRetentionOptions.cs`): configurable, defaults — `HeartbeatDays=30`, `DeviceEventDays=90`, `OperationLogDays=90`, `ProcessedSyncInboxDays=180`, `ExpiredIdentityCredentialDays=30`, `NotificationDeliveryDays=90`; deletes run in bounded batches (`BatchSize=1000`, `MaxBatchesPerRun=20`) rather than one unbounded `DELETE`, matching `docs/data/DATA_MODELING_RULES.md`'s "Retention deletes bounded batches" rule. `NotificationDeliveryDays` exists in code but is **not** mentioned in the doc's high-volume-table retention list — a minor doc/code gap, not a defect.
- **No PostgreSQL native partitioning** exists yet for any table (confirmed by absence of raw-SQL partition DDL in `src/Infrastructure/Migrations/**`) — `docs/data/DATA_MODELING_RULES.md`'s partition-key table is a forward-looking plan, not implemented state.
- **Design-time vs runtime context creation**: `AppDbContextFactory` (`IDesignTimeDbContextFactory<IceBotDbContext>`) is what `dotnet ef migrations add/database update` uses; it layers `appsettings.json` → `appsettings.Development.json` → environment variables, independent of the ASP.NET Core host's own DI-based `AddDbContext` registration.

**Evidence**: `docker/docker-compose.yml`; `src/Infrastructure/AppDbContextFactory.cs`; `src/Infrastructure/DependencyInjection.cs:72-90`; `src/Infrastructure/Data/IceBotDbContext.cs` (whole file); `src/Infrastructure/Data/Configurations/EfModelConfigurationConstants.cs`; `src/Infrastructure/Data/SoftDeleteQueryExtensions.cs`; `src/Infrastructure/Concurrency/PostgresAdvisoryLockManager.cs`; `src/Infrastructure/Persistence/Jobs/DataRetentionOptions.cs`; `src/Infrastructure/Migrations/**` (all 5 migrations + `ManualSteps/*.cs` + `IceBotDbContextModelSnapshot.cs`); `src/Infrastructure/RobotConfiguration/Storage/ObjectStorage/MinioArtifactObjectStorage.cs`.

---

## 8. Evidence Paths (index)

**Core persistence infrastructure**
- `src/Infrastructure/Data/IceBotDbContext.cs` — DbSet registry, global conventions, soft-delete exception list, `OnModelCreating`.
- `src/Infrastructure/Data/Configurations/EfModelConfigurationConstants.cs` — soft-delete filter SQL fragments.
- `src/Infrastructure/Data/SoftDeleteQueryExtensions.cs` — `WhereNotDeleted()` extension.
- `src/Infrastructure/AppDbContextFactory.cs`, `src/Infrastructure/DependencyInjection.cs` — connection/provider wiring.
- `src/Infrastructure/Migrations/*.cs` (+ `ManualSteps/*.cs`, `IceBotDbContextModelSnapshot.cs`) — physical schema history.
- `src/Infrastructure/Concurrency/PostgresAdvisoryLockManager.cs`, `src/Infrastructure/Persistence/Jobs/DataRetentionOptions.cs`.

**Domain base classes / shared abstractions**
- `src/Domain/Common/EntityBase.cs`, `BusinessEntity.cs`, `CatalogEntity.cs`, `RobotConfigurationEntity.cs`, `SyncAggregateEntity.cs`, `EntityAbstractions.cs`, `GuidId.cs`, `Enums/CommonEnums.cs`.

**Per-bounded-context entity + configuration folders**
| Context | Domain entities | EF configuration |
|---|---|---|
| Tenants | `src/Domain/Tenants/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/Tenants/TenantConfigurations.cs` |
| Identity | `src/Domain/Identity/Entities/`, `Enums/`, `ValueObjects/` | `src/Infrastructure/Data/Configurations/Identity/AccountIdentityConfigurations.cs`, `AuthTokenConfigurations.cs` |
| Catalog | `src/Domain/Catalog/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/Catalog/ProductCatalogConfigurations.cs`, `RecipeIngredientConfigurations.cs` |
| Inventory | `src/Domain/Inventory/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/Inventory/InventoryConfigurations.cs` |
| SalesCatalog | `src/Domain/SalesCatalog/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/SalesCatalog/MenuConfigurations.cs` |
| Orders | `src/Domain/Orders/Entities/`, `Enums/`, `Incidents/` | `src/Infrastructure/Data/Configurations/Orders/OrderConfigurations.cs` |
| Payments | `src/Domain/Payments/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/Payments/PaymentConfigurations.cs` |
| Devices | `src/Domain/Devices/Catalog/`, `Connectivity/`, `ExecutionEndpoints/` (+`Projections/`), `Telemetry/` | `src/Infrastructure/Data/Configurations/Devices/DeviceCatalogConfigurations.cs`, `DeviceTelemetryConfigurations.cs`, `ExecutionEndpointConfigurations.cs` |
| RobotConfiguration | `src/Domain/RobotConfiguration/Artifacts/`, `ArtifactTemplates/`, `ArtifactContracts/`, `AuthoringImports/`, `Programs/` (+`Manifests/`) | `src/Infrastructure/Data/Configurations/RobotConfiguration/RobotArtifactTechnicalContractConfigurations.cs`, `RobotProgramAndArtifactConfigurations.cs` |
| ProductionConfiguration/Execution/Packages | `src/Domain/ProductionConfiguration/Entities/`, `Enums/`, `ValueObjects/`, `Manifests/`; `src/Domain/ProductionExecution/Enums/`, `Projections/`; `src/Domain/ProductionPackages/*.cs` | `src/Infrastructure/Data/Configurations/ProductionConfiguration/DeploymentConfigurations.cs`, `ReleaseRouteConfigurations.cs`; `ProductionExecution/ProductionExecutionConfigurations.cs`; `ProductionPackages/ProductionPackageConfigurations.cs` |
| Operations | `src/Domain/Operations/Entities/`, `Enums/` | `src/Infrastructure/Data/Configurations/Operations/OperationConfigurations.cs` |
| Sync | `src/Domain/Sync/DeadLetters/`, `Entities/`, `Enums/`, `Ingestion/` | `src/Infrastructure/Data/Configurations/Sync/EdgeCommandConfigurations.cs`, `SyncDeadLetterConfigurations.cs`, `SyncIngestionConfigurations.cs` |

**Docs cross-referenced (read-only, not modified)**
- `docs/data/DATA_MODELING_RULES.md`, `docs/data/JSON_FIELD_RULES.md`, `docs/architecture/MULTI_TENANCY_RULES.md`.

**Physical deployment**
- `docker/docker-compose.yml`, `docker/Dockerfile`, `src/WebAPI/appsettings.json`, `src/WebAPI/appsettings.Development.json`.

---

## 9. Open Questions / Discrepancies Found During This Inventory

Per `deliverables/DELIVERABLES_AGENT.md`: "When unsure, create an Open Questions section instead of inventing behavior." The following are gaps between code and the narrative that was used to brief the research passes, or internal inconsistencies in the code itself — none were resolved by guessing.

1. **`RobotProgram` has no `TemplateProgramId` field.** `docs/architecture/MULTI_TENANCY_RULES.md` describes RobotProgram as using a `TemplateProgramId` lineage field like Product/Recipe; a repo-wide search under `src/Domain/RobotConfiguration` found zero matches for `TemplateProgramId`. Either the field lives in an unread partial-class file (`RobotProgram` is declared `partial class`) or the doc is aspirational/stale.
2. **`ConfigurationRelease`'s actual unique index differs from the doc's suggested composite.** `docs/architecture/MULTI_TENANCY_RULES.md` suggests `OrganizationId + StoreId + KioskId + DeviceId + RecipeId + Code`; the real index in `ReleaseRouteConfigurations.cs` is `(OrganizationId, ReleaseNumber)` filtered not-deleted. `ConfigurationRelease` has no `StoreId`/`KioskId`/`DeviceId`/`RecipeId`/`Code` columns at all — those scopes live one level down, on `ExecutionRoute`/`Recipe`/`RobotProgram`.
3. **`EdgeCommandDeliveryAttempts` is missing its documented time index.** `docs/data/DATA_MODELING_RULES.md` names it a high-volume table with partition key `SentAt` and requires a time-based index; the only index present in `EdgeCommandConfigurations.cs` is the unique `(EdgeCommandId, DeliveryAttemptNo)`. No index contains `SentAt`.
4. **`ProductionEventCheckpoints` and `EdgeStateSummaries` are structurally bounded (upsert-per-executor), not append-only logs**, despite being listed alongside genuinely high-volume tables in `docs/data/DATA_MODELING_RULES.md`. Each has a unique index enforcing one row per executor (or per executor+kind), and neither appears in the doc's own partition-key table — internally consistent, but worth a reviewer's confirmation that the doc's grouping is intentional.
5. **`ProductOption.TemplateProductOptionId`** exists as a scalar column but has **no `HasOne(...)` FK configured** in `ProductCatalogConfigurations.cs`, unlike `Product.TemplateProductId` and `Recipe.TemplateRecipeId`, which are both explicitly configured as FKs. Not confirmed whether this is intentional (soft reference only) or an oversight.
6. **Possible silent override of explicit `Cascade` delete behaviors.** `IceBotDbContext.ConfigureEntityConventions` iterates every FK in the model and force-sets `DeleteBehavior.Restrict` (`IceBotDbContext.cs:269-273`) *after* `ApplyConfigurationsFromAssembly` runs. Several configuration classes explicitly set `DeleteBehavior.Cascade` for genuine parent-owns-child rows (e.g. `RobotArtifactTechnicalContract → RobotArtifactDeclaredEffect/OrderingConstraint`, `RobotAuthoringImport → RobotAuthoringImportItem`, `ProductionPackage*` parent/child pairs, `ControllerArtifactSetDeployment → Item`). Whether the later loop actually reverts these to `Restrict` was not settled by static reading of the C# source alone — confirming would need either a query against the generated migration SQL/model snapshot's `onDelete` values, or a runtime check. Flagging rather than asserting either way.
7. **Inconsistent base class between structurally-parallel append-only history tables.** `OrderStatusHistory` inherits `BusinessEntity` (gets audit + soft-delete columns); `OrderItemStatusHistory` and `ProductionIncidentHistory` inherit a bare `GuidEntity` (no audit columns at all) despite serving the identical "append-only transition log" role. Not necessarily a bug, but a real inconsistency visible directly in the entity files.
8. **`Device.MetadataJson` and `DeviceModel.MetadataJson` have no accompanying schema-version field**, unlike `DeviceModel.CapabilitiesJson` (paired with `CapabilitiesSchemaVersion`) — consistent with `docs/data/JSON_FIELD_RULES.md`'s rule that plain `MetadataJson` fields are non-critical extension points and need no version, but worth noting as a real asymmetry within the same entity.
9. **`ExecutionRoute.RequiredCapabilitiesJson`** is described in `docs/data/JSON_FIELD_RULES.md` as always using a fixed internal `"schemaVersion": 1` envelope inside the JSON payload itself — but there is no dedicated schema-version *column* on `ExecutionRoute` the way other source-of-truth JSON fields have (e.g. `ManifestSchemaVersion`, `InputSchemaVersion` elsewhere). This may be intentional (version is embedded in the JSON body, not a sibling column) rather than a gap — flagged for confirmation, not asserted as a defect.
10. **Two different configuration keys for the DB connection string** depending on code path: `CONNECTIONSTRING` (runtime DI, `DependencyInjection.cs:76`) vs `ConnectionStrings:IceBot_DB` / `ConnectionStrings__IceBot_DB` (design-time factory, `AppDbContextFactory.cs:22-23`). Both ultimately point at the same physical database in normal operation, but this is a real divergence in how the two paths are configured, worth a reviewer's awareness when debugging environment-specific connection issues.
11. **`ExecutionEndpointCredentialBinding.PublicKeyPem`** has no `HasMaxLength` override in `ExecutionEndpointConfigurations.cs`, so it inherits the global 500-character string default — unusually short for a PEM-encoded public key. Not resolved whether this is an intentional design constraint (e.g., only a fingerprint is stored) or an oversight, since `mTLS` mode is documented to use a certificate fingerprint as `CredentialReference` instead of a key, but `PublicKeyPem` is specifically for the alternate signed-command mode.

These items are handed to reviewers as-is; none were altered or "corrected" by inference, per the source-priority rule in `deliverables/DELIVERABLES_AGENT.md`.
