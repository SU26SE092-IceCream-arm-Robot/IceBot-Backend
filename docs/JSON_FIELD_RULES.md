# JSON Field Rules

JSON columns are acceptable in this domain because the system runs across edge kiosks, robot SDKs, payment providers, and cloud sync. They must not all be treated the same. Every JSON field should fall into one of these roles.

## Search Keywords

`JSON fields`, `JSONB`, `ConfigJson`, `SettingsJson`, `ParametersJson`, `SnapshotJson`, `PayloadJson`, `HeadersJson`, `RawRequestJson`, `RawResponseJson`, `MetadataJson`, `schema version`, `source of truth JSON`, `immutable snapshot`, `append-only payload`, `provider payload`, `robot parameters`, `sync payload`, `JSON conflict resolution`

## Roles

### Source of truth configuration

These fields can affect runtime behavior. They are mutable only while the owning aggregate is in a draft/provisioning state. After publish/activation, update by creating a new business version or increasing the entity configuration/version field.

Fields:

- `PaymentMethod.ConfigJson` with `ConfigSchemaVersion`
- `DeviceModel.CapabilitiesJson` with `CapabilitiesSchemaVersion`
- `Kiosk.SettingsJson` with `SettingsSchemaVersion`
- `Store.OpeningHoursJson` with `OpeningHoursSchemaVersion`
- `Recipe.InstructionsJson` with `InstructionsSchemaVersion`
- `RobotProgram.ProgramPayloadJson` with `ProgramPayloadSchemaVersion`
- `RobotProgram.PointSnapshotJson` with `PointSnapshotSchemaVersion`
- `RobotProgram.SafetyZoneJson` with `SafetyZoneSchemaVersion`
- `RobotProgramStep.ParametersJson` with `ParametersSchemaVersion`
- `RobotProgramStep.ParametersOverrideJson` with `ParametersOverrideSchemaVersion`
- `RobotProgramStep.RetryPolicyJson` with `RetryPolicySchemaVersion`
- `RobotProgramStep.PointSnapshotJson` with `PointSnapshotSchemaVersion`
- `KioskRecipeExecutionProfile.ResolverPolicyJson` with `ResolverPolicySchemaVersion`
- `KioskRecipeExecutionProfile.ExecutionSnapshotJson` with `ExecutionSnapshotSchemaVersion`
- `IngredientDispenserState.LevelToQuantityProfileJson` with `LevelToQuantityProfileSchemaVersion`

Rules:

- Do not put query-critical fields only inside JSON. Promote them to typed columns.
- Validate JSON against the matching schema version before publish, activation, or deployment to edge.
- Edge sync conflict resolution must use the owning aggregate version, not JSON diffing.
- Robot programs and steps should be shipped to edge as a complete versioned package, not as ad hoc realtime step edits.

### Immutable snapshot

These fields preserve the state used at the time of an order or robot job. They are not source of truth after creation, but they are required for audit, replay, refunds, and reporting.

Fields:

- `OrderItem.OptionsJson` with `OptionsSchemaVersion`
- `OrderItem.RecipeSnapshotJson` with `RecipeSnapshotSchemaVersion`
- `RobotJob.RecipeSnapshotJson` with `RecipeSnapshotSchemaVersion`
- `RobotJobStep.ParametersJson` with `ParametersSchemaVersion`
- `PaymentTransaction.RawRequestJson`
- `PaymentTransaction.RawResponseJson`

Rules:

- Treat as immutable once the order item, robot job, or payment attempt is created.
- If product, product variant, recipe, or option configuration changes later, do not rewrite historical snapshots.
- Reports may read snapshots for historical truth, but current catalog pages should read typed product/product variant/recipe tables.

### Append-only payload/debug

These fields are evidence from external systems, sync, or runtime events. They are useful for troubleshooting and replay, but should not drive core business decisions unless normalized into typed fields first.

Fields:

- `PaymentCallback.PayloadJson`
- `SyncEventInbox.PayloadJson`
- `SyncEventInbox.HeadersJson`
- `SyncDeadLetter.PayloadJson`
- `DeviceEvent.PayloadJson`
- `RobotJobEvent.PayloadJson`
- `OperationLog.PayloadJson`
- `KioskHeartbeat.PayloadJson`
- `IngredientDispenserState.SensorPayloadJson`

Rules:

- Treat as append-only or last-observation debug data depending on the owning entity.
- Idempotency must use typed keys such as provider event id, event id, source node id, or correlation id.
- Retry logic must use typed retry/status columns, not parsed JSON state.
- If a value becomes operationally important, add a typed column and backfill from payloads if needed.

### Metadata

Metadata JSON is for non-critical vendor extensions, display hints, and integration-specific extra data.

Fields:

- `Organization.MetadataJson`
- `Product.MetadataJson`
- `ProductOption.MetadataJson`
- `Ingredient.MetadataJson`
- `Device.MetadataJson`
- `DeviceModel.MetadataJson`

Rules:

- Do not use metadata as a hidden domain model.
- Do not require metadata for checkout, robot execution, stock calculation, payment state, tenant isolation, or authorization.
- Promote repeated, queried, indexed, or validated keys to typed columns.

## Naming

- `*ConfigJson`, `*SettingsJson`, `*ParametersJson`, `*InstructionsJson`, `*ProfileJson`: source of truth configuration, must have an explicit schema version.
- `*SnapshotJson`: immutable historical copy.
- `PayloadJson`, `HeadersJson`, `Raw*Json`: external evidence/debug payload.
- `MetadataJson`: optional extension data only.

## Sync Boundary

For edge-cloud sync, conflict resolution should happen at the aggregate boundary:

- Robot configuration: `RobotProgram`, `RobotProgramStep`, and `KioskRecipeExecutionProfile` versions.
- Runtime execution: `RobotJob`, `RobotJobStep`, and append-only runtime events.
- Stock reporting: typed `StockMovement` quantities; JSON sensor payloads are only supporting evidence.

Never resolve sync conflicts by merging arbitrary JSON payloads from cloud and edge. Either reject stale writes, create a new version, or normalize the changed field into typed columns.

## Related Docs

- [Data Modeling Rules](DATA_MODELING_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [IoT Contract](IOT_CONTRACT.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
