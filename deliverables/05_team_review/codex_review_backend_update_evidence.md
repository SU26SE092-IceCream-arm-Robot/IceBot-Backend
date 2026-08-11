# Codex Review — Backend Update Evidence After 2026-08-11 Sync

## Review Scope

Reviewed evidence:

- `deliverables/00_repo_evidence/backend_update_impact_2026-08-11.md`
- `deliverables/00_repo_evidence/repo_truth_map.md`
- `deliverables/00_repo_evidence/functional_inventory.md`
- `deliverables/00_repo_evidence/database_inventory.md`

Compared against the merged repository at current branch merge commit `3f855e9`, including `AGENTS.md`, `ARCHITECTURE.md`, relevant current files under `docs/`, `src/`, migrations, and changed tests.

This is a review only. No reviewed evidence file, source file, backend document, test, baseline deliverable, or school report was modified. No build, test execution, migration, or external integration was run.

## Overall Assessment

**Result: Major update required before downstream documentation can be treated as current.**

`backend_update_impact_2026-08-11.md` is a useful and substantially evidence-supported change bridge. It correctly identifies the principal upstream changes and the downstream documentation order. However, the other three evidence baselines have not been synchronized with those changes:

- none contains `InventorySensorObservation`, `inventory-observations`, `ProductionProgramBinding`, `/me/sessions`, `raw-lua-artifacts`, runtime-menu caching, or the three new migration identifiers;
- `functional_inventory.md` still publishes removed account routes and superseded role/permission rules;
- `database_inventory.md` still describes the pre-sync five-migration model; and
- `repo_truth_map.md` still presents the earlier module/controller/flow summary.

The impact analysis should remain clearly identified as a delta analysis. It does not make the stale inventories current by reference.

## Findings by Priority

### Critical — RBE-01: Core inventories were not updated after the merge

**Files:** `repo_truth_map.md`, `functional_inventory.md`, `database_inventory.md`

All three retain their pre-sync size/structure and contain zero matches for the primary new implementation identifiers. This directly conflicts with current source and with the impact analysis.

Missing concepts include:

- current-account session listing and per-session revocation;
- organization-owned account administration and assignable-role options;
- inventory sensor observation ingestion and persistence;
- runtime-menu projection cache/fallback/metrics;
- Production Program Binding lifecycle, API, entity, and persistence;
- raw Lua import into an existing Draft RobotProgram;
- robot-authoring import listing and revised declaration/composition semantics;
- execute-order payload schema v5 and capability-code arrays;
- deployment audit/concurrency/reason behavior;
- maintenance assignee options;
- verified-unmatched payment webhook handling; and
- new CI, image publication, deployment, and local-development bootstrap behavior.

**Required correction:** Regenerate/review the three inventories from the merged revision before updating the SRS, RTM, UML, database designs, or school reports.

### Critical — RBE-02: Functional inventory publishes obsolete account routes

**File:** `functional_inventory.md`, Identity rows IDN-15 and IDN-17–IDN-25

The inventory still states routes under:

```text
/api/v1/management/accounts...
```

Current `ManagementAccountsController` is routed under:

```text
/api/v1/management/organizations/{organizationId}/accounts...
```

The organization identifier is now a material ownership and authorization boundary, not a cosmetic route prefix. Commands/queries receive it and enforce organization-scoped account access.

**Required correction:** Update every account list/detail/create/update/disable/password/role/effective-access/invitation route, input, actor, business rule, and evidence reference. Treat this as a breaking contract unless the team supplies separate compatibility evidence.

### Critical — RBE-03: Functional inventory contains contradicted identity authorization claims

**File:** `functional_inventory.md`, IDN-18–IDN-26 and related notes

Outdated claims include:

- Manager access to account directory/management paths;
- account mutation described as SystemAdmin-only in some rows;
- role assignment described through the former SystemAdmin/OrgAdmin/Manager hierarchy;
- `GET /api/v1/management/roles` with policy `roles.view`; and
- effective-access responses described without the new organization-specific boundary.

Current rules establish:

- `accounts.read` and `accounts.manage` for SystemAdmin and constrained OrgAdmin;
- no Manager account-directory role in those policies;
- organization-owned account results and mutations;
- `GET /api/v1/management/accounts/assignable-role-options` for account-authoring choices;
- platform-wide permission matrix restricted to SystemAdmin through `permission-matrix.view`; and
- `GET /api/v1/me/access` returning `permissionCodes` for capability checks.

**Required correction:** Rewrite these rows from the current controllers, `AccountManagementAccessRules`, handlers, authorization registration, and `docs/api/AUTHORIZATION_RULES.md`. Do not preserve the old actor list as backward compatibility without evidence.

### Critical — RBE-04: Database inventory is materially stale

**File:** `database_inventory.md`

The inventory states:

- approximately 130 DbSet properties;
- five migrations; and
- approximately 99 tables created across migration history.

Current static counts are:

- **100** `DbSet<...>` declarations in `IceBotDbContext.cs`;
- **eight** non-designer migration classes, excluding the model snapshot; and
- **101** `migrationBuilder.CreateTable` calls across non-designer migrations.

The three omitted migrations are:

1. `20260731040709_AddInventorySensorObservations`
2. `20260804031725_AddProductionProgramBindings`
3. `20260809035315_DeriveProductionBindingCapabilitiesFromContracts`

The inventory also omits the new DbSets/entities, relationships, JSONB fields, indexes, capability migration, and model-snapshot changes.

**Qualification:** Create-table call count is migration-history evidence, not a verified live-table count. The corrected inventory should retain that distinction.

### High — RBE-05: `InventorySensorObservation` is absent from all three core inventories

Current code/migration establishes a new Inventory entity and Edge ingestion flow with:

- endpoint/executor/source-event/dispenser/device/ingredient identities;
- unique `(SourceExecutorId, SourceEventId)` idempotency;
- per-executor/dispenser sequencing;
- applied, duplicate, and out-of-order handling;
- observed/Cloud-received timestamps and bounded sensor JSON evidence;
- optional calibration-derived quantity;
- restrictive endpoint/dispenser FKs; and
- inventory-change realtime publication after successful application.

The MQTT message family `inventory-observations` accepts 1–100 observations. It does not create a stock movement, prove recipe consumption, or control v1 menu/checkout sellability.

**Required correction:** Add the entity to database inventory; add the MQTT/Inventory function to functional inventory; add the evidence flow and boundary to the truth map. Decide in the SRS/RTM whether this extends existing inventory requirements or needs a new requirement ID `[Needs Review]`.

### High — RBE-06: `ProductionProgramBinding` and revised capability evidence are absent

Current code establishes an immutable, operator-confirmed organization-owned binding between Recipe/version, ProductVariant, Published RobotProgram/manifest, supported option codes, declared required capability codes, assurance, and evidence status. It has Active/Retired lifecycle and a unique binding checksum.

Current routes provide list, create, and retire operations under the organization. Execution-route robot bindings can snapshot the production binding identity/checksum and required capability-code array.

The later migration intentionally maps pre-existing production bindings to `CapabilityEvidenceStatus.Missing`; assurance is currently `OperatorDeclared`. Optional technical declarations are declarations, not proof or certification of Lua behavior.

**Required correction:** Add the entity/table/API/business flow and revise `ExecutionRouteRobotBinding` attributes, relationships, JSON roles, indexes, authoring flow, and Edge dispatch description.

### High — RBE-07: Robot authoring evidence retains the former technical-contract boundary

**Files:** `repo_truth_map.md`, `functional_inventory.md`, `database_inventory.md`

The inventories describe technical contracts as required behavior provenance in several authoring/package rows. Current implementation/docs changed this boundary:

- an artifact may be published without a technical declaration;
- when present, the declaration must pass scope/status/checksum validation;
- declaration metadata is not behavior certification;
- normal Recipe-to-Program confirmation is the Production Program Binding lifecycle;
- import-local composition/release creation remains an advanced automation path; and
- raw Lua files/archive may be imported directly into a Draft RobotProgram.

`functional_inventory.md` also lacks the new list-import route and `expectedLastModifiedAt` conflict protection for replacing program artifact order.

**Required correction:** Re-audit Robot Configuration rows RC-03 onward, Production Configuration rows, and package definitions for assumptions that every artifact has a required certified technical contract.

### High — RBE-08: Edge/IoT evidence is missing schema v5 and the new uplink family

`repo_truth_map.md` and `functional_inventory.md` still summarize the earlier command and MQTT set.

Current changes include:

- `ExecuteOrderCommandPayload.SchemaVersion = 5` by default;
- supported decoding of versions 3, 4, and 5;
- `RequiredCapabilityCodes` per robot-program binding instead of one scalar capability;
- dispatch validation across every required code; and
- MQTT `inventory-observations` dispatch.

**Required correction:** Update IoT/MQTT rows, dispatch payload descriptions, Edge sequence evidence, compatibility notes, and tests. Keep Edge rollout compatibility `[Needs Review]`; current code acceptance of old payload versions does not prove all external Edge runtimes accept v5.

### High — RBE-09: Runtime-menu cache behavior and NFR evidence are missing

Current code/docs establish an optional runtime-menu projection cache, dedicated projection builder, fallback to database on cache failure, bounded cache behavior, revision handling, and cache metrics. Kiosk/store admission is evaluated before reading the cached projection, and checkout remains the transactional authority.

This is absent from the truth map and functional inventory.

**Required correction:** Update Sales Catalog/runtime menu, architecture/infrastructure, configuration, reliability, observability, and test evidence. Exact TTL/profile/configuration and invalidation details should be copied from current code/docs rather than generalized. Do not state that cache availability guarantees checkout availability without executed evidence.

### High — RBE-10: Configuration deployment and release-authoring rows are stale

**File:** `functional_inventory.md`, PC-02 and PC-04–PC-09

The current behavior includes:

- release revision/concurrency tokens;
- production-binding references and capability arrays;
- richer authoring options including production-affecting options and declared capability indexes;
- deployment requests requiring an operator reason;
- audit records containing actor and matching authorization scope;
- rollback requiring a client-observed active deployment ID; and
- conflict rejection for stale rollback observation.

Existing rows still describe the earlier input/output shapes and omit these controls.

**Required correction:** Update the route-authoring, options, preview, Full Edge, low-cost, rollback, monitoring, and package-upgrade interactions together. Verify transaction/idempotency/partial-failure behavior instead of treating audit logging as sufficient completion evidence.

### High — RBE-11: Database relationships, indexes, and JSON classification are incomplete

`database_inventory.md` must add at minimum:

- `InventorySensorObservation` FKs/indexes and diagnostic `SensorPayloadJson`;
- `ProductionProgramBinding` FKs, unique checksum, lifecycle/status, supported-option JSON, required-capability JSON, assurance/evidence fields, and soft-delete behavior;
- optional `ExecutionRouteRobotBinding.ProductionProgramBindingId` FK and binding checksum;
- `ExecutionRouteRobotBinding.RequiredCapabilityCodesJson` historical snapshot behavior; and
- the migration that converts the historical scalar route capability into a JSON array while removing the scalar from Production Program Binding.

The current multi-tenancy lists also omit Production Program Binding from required-`OrganizationId` entities.

### Medium — RBE-12: Operations API evidence is incomplete

Missing or changed behavior includes:

- maintenance-ticket assignee options restricted to eligible active Technician/Manager accounts matching the ticket's derived scope;
- assignment eligibility revalidated at mutation time;
- distinct `notifications.view` and `notifications.manage` permissions; and
- normal notification delivery reads excluding content/provider diagnostics.

Update Operations rows and actor/authorization summaries. Do not infer a frontend screen from these backend routes.

### Medium — RBE-13: Payment webhook exception behavior is missing from core inventories

Current behavior verifies the provider signature before accessing local payment/order data. A verified callback with no matching `(Provider, ProviderOrderCode)` is acknowledged with 2xx, creates no PaymentCallback or financial/fulfillment state, and records safe operational evidence through logging and `icebot.payment.webhook.verified_unmatched`.

**Required correction:** Add this alternative flow to Payments, observability, and test evidence. Keep alert thresholds, retention, and operator runbook `[Needs Review]`.

### Medium — RBE-14: Local bootstrap and CI/CD evidence is not integrated

The core inventories omit:

- development-only role-account and execution-endpoint seeds;
- Vanilla soft-serve catalog fixture and robot-authoring automation reset;
- pull-request restore/build/test/Docker-build workflow using .NET 10;
- main-branch GHCR image build/push and production deployment through NetBird/SSH;
- `.dockerignore`; and
- removal of the prior deployment workflow.

These are operational/quality facts, not product functions. Route them to evidence summaries, test/quality evidence, Report 6 installation/release content, and future team-owned Report 2 configuration management. Do not claim successful CI/deployment without immutable run evidence.

### Medium — RBE-15: Source citations and line references are stale

Many functional rows cite controller line ranges that moved after merged edits. Several source files now have new constructor dependencies, route prefixes, inputs, or handlers. The truth map also states that controller contents were not opened, which is no longer adequate for a post-sync evidence baseline.

**Required correction:** Recreate citations from the merged revision and record the source commit. Prefer stable symbol/file references plus line numbers generated from that frozen revision.

### Medium — RBE-16: `Implemented` is defined too strongly

`functional_inventory.md` defines `Implemented` as “working code wired end-to-end.” Static inspection can support “implemented and wired in source,” but it cannot establish working runtime behavior, migration success, external compatibility, or test pass status.

**Required correction:** Narrow the definition or attach execution evidence. Suggested wording: “Implementation and wiring found in the reviewed source revision; runtime success is not asserted unless cited test/execution evidence is provided.”

### Medium — RBE-17: Repo truth map contains stale or over-compressed flow statements

Examples:

- the controller list omits `ManagementProductionProgramBindingsController`;
- Inventory ownership omits sensor observations;
- Production Configuration ownership omits Production Program Bindings;
- the business-flow list omits session security, production binding, inventory observation, and cache behavior;
- the IoT concrete-route summary does not mention the MQTT-only inventory observation channel; and
- the checkout flow says Edge executes the robot program and Cloud finalizes `Order=Completed` without preserving enough distinction between Edge-reported evidence and independent physical proof.

The last statement is not necessarily false as a software-state flow, but it needs the established qualification: Cloud applies accepted Edge evidence and does not independently verify physical output.

### Medium — RBE-18: Existing database open questions need revalidation

The prior open questions may still be valuable, but line references and model assumptions must be rerun against the new snapshot. In particular:

- delete-behavior interaction must be verified against the current model;
- `ExecutionRoute.RequiredCapabilitiesJson` now coexists with required-capability arrays on route robot bindings and production bindings;
- migration/model counts changed; and
- new entities may change high-volume/retention/index review priorities.

Do not delete unresolved historical findings merely because the model changed; mark each confirmed, resolved, superseded, or still open with current evidence.

## Review of `backend_update_impact_2026-08-11.md`

### Correctly reflected findings

The impact analysis correctly identifies the major merged changes:

- session list/revoke and transactional password/session revocation;
- organization-owned account routes and constrained OrgAdmin management;
- inventory sensor observation entity/flow;
- runtime-menu caching and fallback;
- raw Lua import and robot-authoring changes;
- Production Program Binding and capability evidence;
- execute-order schema v5/capability arrays;
- deployment audit/concurrency changes;
- verified-unmatched payment callback behavior;
- maintenance/notification authorization changes;
- three migrations and model changes;
- added/changed tests; and
- CI/GHCR/deployment automation.

Its downstream impact and recommended ordering are also appropriate.

### Corrections / qualifications needed

1. The analysis says the upstream change set contains 25 commits. This is consistent with Git's branch-behind count including merge commits, while the non-merge commit list is smaller. Add “including merge commits” to remove ambiguity.
2. “Redis/HybridCache-style” should be replaced with the exact currently registered cache stack and configuration from source/docs when the evidence inventory is regenerated. The impact document appropriately marks remaining details for review, so this is a precision improvement rather than an unsupported feature claim.
3. The statement that existing Production Program Bindings migrate to missing evidence is supported by migration default `CapabilityEvidenceStatus = 1` and the enum mapping `Missing = 1`. Cite both together in the refreshed database inventory.
4. The analysis correctly describes pipeline definitions but must continue to avoid implying successful runs. No pipeline run or deployment result was reviewed.
5. The API section should explicitly list `GET /api/v1/management/accounts/assignable-role-options` as a new/replacement operation, rather than only mentioning it in changed-contract prose.
6. `[Needs Review]` Trace whether `inventory-observations` has full dead-letter/replay/operator recovery behavior. The impact analysis already marks this uncertain and should not be strengthened without evidence.

No material invented backend feature was found in the impact analysis. Its uncertainty labels are generally appropriate.

## Uncertainty-Label Review

### Appropriate usage

`backend_update_impact_2026-08-11.md` uses `[Needs Review]` and `[Unclear]` for requirement-ID decisions, client compatibility, dead-letter/replay coverage, cache/deployment operations, provider runbooks, CI results, and Report 2 ownership. These are proper unresolved items.

### Problems in the older inventories

- Stale route and permission claims are presented as `Implemented`, not as uncertain or superseded.
- `repo_truth_map.md` open questions describe limited reading scope but do not mark the body as a pre-sync snapshot.
- `database_inventory.md` presents obsolete counts as current facts.
- Several physical/operational statements are inferred from static structure without the bracketed uncertainty labels required for unresolved behavior.

**Required correction:** Add a source revision and “current as of” header to every evidence file. Until regeneration is complete, mark the three core inventories `[Outdated — Needs Review]` at document level rather than adding uncertainty labels to every stale row.

## Missing API / Entity / Flow Checklist

The regenerated evidence should explicitly account for all of the following:

- [ ] `GET /api/v1/me/sessions`
- [ ] `DELETE /api/v1/me/sessions/{sessionId}`
- [ ] organization-owned account route family
- [ ] assignable-role options and restricted permission matrix
- [ ] maintenance-ticket assignee options
- [ ] notification delivery view/manage separation
- [ ] Production Program Binding list/create/retire
- [ ] robot-authoring import list
- [ ] raw Lua artifact import into a Draft program
- [ ] optimistic concurrency for program artifact ordering
- [ ] configuration release revision/concurrency and production-binding fields
- [ ] deployment/rollback reason, audit scope, and stale-observation protection
- [ ] runtime-menu projection cache/fallback/revision/metrics
- [ ] `InventorySensorObservation` entity/table and MQTT message family
- [ ] execute-order payload schema v5 and capability arrays
- [ ] verified-unmatched PayOS callback metric/no-state branch
- [ ] development-only bootstrap/fixture/reset behavior
- [ ] migrations, model snapshot, indexes, JSONB fields, and current counts
- [ ] added/changed tests and pipeline definitions without claiming results

## Baseline Deliverables That Must Be Updated Next

Update only after the three core evidence inventories are corrected:

1. `deliverables/02_srs/srs.md`
   - identity sessions, organization account administration, permissions;
   - inventory observation ingestion;
   - robot authoring/technical declaration/production binding;
   - runtime-menu cache/fallback;
   - deployment audit/concurrency;
   - payment unmatched callback; and
   - operations lookups/diagnostics.

2. `deliverables/02_srs/requirements_traceability_matrix.md`
   - update route/symbol/test references and decide FR extensions versus new IDs.

3. `deliverables/03_uml/*.md`
   - use cases, class/ERD, order/payment and robot/Edge sequences, and activity exceptions.

4. `deliverables/04_database_design/*.md`
   - conceptual/logical/physical representation of both new entities and changed route-binding schema.

5. `deliverables/01_project_introduction/project_introduction.md`
   - only stable high-level product/architecture changes after SRS/design approval.

6. `deliverables/05_team_review/open_questions.md` and review checklists
   - merge the new compatibility, authorization, cache, migration, deployment, and evidence questions.

## School Reports That Must Be Updated

Recommended order:

1. **Report 3 — SRS:** highest requirement/API/actor impact.
2. **Report 4 — SDD:** new entities, cache component, production binding, migrations, diagrams, capability model, deployment behavior.
3. **Report 5 — Test Documentation:** add/update planned cases and CI/tool evidence; keep results `[To Be Updated After Test Execution]`.
4. **Report 6 — User Guides:** session/account/role workflows, maintenance assignment, raw Lua and binding workflows, installation/configuration/cache/CI/CD information; UI/hardware details remain `[Needs Team Review]`.
5. **Report 1 — Project Introduction:** concise stable scope changes only.
6. **Report 7 — Final Project Report:** recompile last from the approved updated component reports.

Report 2 remains team-owned. Pipeline/configuration-management facts may be inputs, but schedules, assignments, process results, and approvals must not be inferred.

## Recommended Evidence Remediation Order

1. Record the exact merged source revision in each evidence file.
2. Regenerate the API/data/IoT/docs evidence packs from that revision.
3. Rewrite `repo_truth_map.md` at summary level.
4. Update `functional_inventory.md`, preserving stable IDs where behavior is an extension and allocating new IDs only after SRS/RTM review.
5. Update `database_inventory.md` from current entities, configurations, migrations, and model snapshot; report migration-history counts separately from live-schema facts.
6. Cross-check all routes against current controllers/OpenAPI, all entities against DbSets/configurations, and all tests against actual test files.
7. Review this remediation independently before changing downstream deliverables.

## Open Review Questions

1. `[Needs Review]` What exact source commit should become the new documentation baseline: merge commit `3f855e9` or a later reviewed commit?
2. `[Needs Review]` Will clients receive a coordinated breaking-route update for organization-owned account APIs?
3. `[Needs Review]` Should session management and inventory observations receive new requirement IDs?
4. `[Needs Review]` Is Production Program Binding the approved normal authoring boundary for every client, with import-local release linkage documented only as advanced automation?
5. `[Needs Review]` Which Edge versions accept execute-order schema v5, and what rollout evidence exists?
6. `[Needs Review]` What cache technology/profile/configuration and invalidation contract is approved for each environment?
7. `[Needs Review]` What replay/dead-letter and operator recovery applies to inventory observations?
8. `[Needs Review]` Where are CI run results, test reports, image digests, deployment records, and rollback/acceptance evidence retained?
9. `[Needs Review]` Has the eight-migration model been applied/reconciled against any target database?
10. `[Unclear]` Which old database open questions are resolved, superseded, or still active under the new model?

## Review Conclusion

The impact analysis is suitable as a delta guide with the qualifications above. The repo truth map, functional inventory, and database inventory are not yet suitable as current post-sync evidence. Downstream baseline and school-report updates should wait until those three files are regenerated and independently checked against the merged source revision.
