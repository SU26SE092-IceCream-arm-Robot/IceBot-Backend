# Production Package Upgrade Flow

This document owns preview, materialization, review, cutover, rollback, abandonment, and reconciliation for upgrading an installed Production Package.

## Search Keywords

`production package upgrade`, `upgrade preview`, `previewChecksum`, `materialization`, `cutover`, `rollback`, `abandon upgrade`, `stale upgrade reconciliation`, `menu rollback evidence`, `successor installation`

## Upgrade Lifecycle

### Preview And Materialization

Upgrade applies only to an `Installed`, `PackageManaged` source installation and
a newer Published version of the same ProductionPackage. Preview compares the
immutable source manifest, current organization-owned commercial state, and the
incoming manifest. It returns added, removed, and changed Product source keys,
affected MenuItems, required execution endpoints, blockers, warnings, and a
deterministic `previewChecksum`.

Preview details identify each Product change, MenuItem rebind/deactivation,
Artifact reuse/materialization action, and frozen execution endpoint. Artifact
preview marks a `ReuseExistingCandidate` from package provenance, immutable
checksum, template, and technical-contract metadata. Execute still performs the
authoritative object size/checksum validation before reuse; publication status
alone does not decide reuse.

Execute requires the preview checksum and `Idempotency-Key`. Backend creates a
successor installation with new Catalog identities and reserved staging Product
and RobotProgram codes. It reuses only exact package-managed Artifact content
allowed by normal installation rules. Product/Variant/Option names, prices,
images, display order, and valid defaults are copied from the source graph;
Product/Variant preparation baselines and Recipe duration come from the incoming
package. MenuItem preparation time remains an organization-owned override.

If execution is interrupted, retrying the same payload and `Idempotency-Key`
resumes the same Upgrade and deterministic successor installation. A successor
identity is persisted before preparation evidence is written, so failure after
materialization cannot create a second Catalog or robot graph. Terminal retries
return the existing Upgrade. Reusing the key with another payload returns
`409`.

Execute revalidates the approved `previewChecksum` immediately before successor
materialization and again before preparation evidence is recorded. A changed
source catalog, MenuItem binding, or active endpoint scope is rejected as stale;
it is never silently added to the approved upgrade.

The successor stays independently reviewable. Existing Recipe, Artifact,
RobotProgram, ConfigurationRelease publication, and deployment APIs remain the
only way to publish and activate its technical graph. Upgrade cutover does not
bypass those lifecycle gates.

### Abandonment And Reconciliation

An operator may abandon a `ReadyForReview` or `Failed` upgrade through
`POST .../{upgradeId}/abandon` with a required reason. This keeps the source
installation active, marks the successor installation `Abandoned`, soft-deletes
its Product, RobotProgram, and ConfigurationRelease roots, and preserves
materialization and artifact provenance for audit. The command is idempotent.
Completed upgrades must use rollback instead.
Abandon returns `409` while successor Products are referenced by MenuItems or
the successor release has a non-failed deployment. Operators must first remove
the premature binding or restore deployment to the source release.

A background reconciler changes a `Materializing` upgrade with no persisted
progress within `ProductionPackageUpgrade:Reconciliation:MaterializingTimeoutMinutes`
to `Failed/UpgradeMaterializationTimedOut`. It does not infer a cutover or Edge
failure. The same idempotent execution may resume the successor, or an operator
may abandon it.

### Cutover

Cutover requires:

- the preview/materialization evidence still matches;
- both source and successor installations remain package-managed;
- the successor release is Published;
- every execution endpoint snapshotted by the upgrade points to an Active
  deployment row owned by the same organization, kiosk, endpoint, profile, and
  successor release;
- MenuItem bindings, allowed options, Product codes, and availability still
  match their typed before evidence.

Cutover runs under one database transaction and advisory locks. It moves source
Product codes to reserved historical codes, assigns canonical codes to the
successor, applies preserved availability, rebinds continuing MenuItems by
package source key, marks removed offerings Unavailable, and supersedes the
source installation. New package Products are not inserted into a Menu
automatically.

### Rollback

Rollback is two-phase and requires an operator reason. The first call creates
idempotent rollback deployments through the existing deployment rollback
handler and returns `202` while Edge activation is pending. Unknown, Pending,
Installed, or Active observation is never redispatched automatically. If an
observed rollback deployment is Failed, another call may create the next audited
deployment attempt, up to three attempts per endpoint. Recording each returned
deployment is serialized per upgrade endpoint, so duplicate idempotent dispatch
reuses the existing audit entry rather than creating another attempt. A repeated
call after every latest rollback deployment is Active verifies after-state checksums,
restores typed MenuItem/option bindings, availability, and canonical Product
codes, restores the source installation, and supersedes the successor.
Post-cutover Catalog or Menu binding changes cause `409`; rollback never
overwrites them silently.

Upgrade detail returns frozen endpoint targets, current rollback deployment
status/failure, and every rollback attempt with attempt number, replaced
deployment, actor, reason, and request time. Clients do not reconstruct this
audit history from deployment lists.

### Ownership And Concurrency

One Upgrade owns one source installation, one tenant scope, and a frozen Product,
MenuItem, and endpoint set. Fleet rollout coordinates multiple independent
upgrades outside this aggregate. A source installation can have only one active
upgrade. `OrganizationFork` installations require an explicit manual rebase and
cannot use this workflow.

Forking is rejected while an installation is either the source or successor of
an upgrade in `Materializing`, `ReadyForReview`, or `RollbackPending`. The guard
is rechecked under the same technical-resource mutation lock used by upgrade
execution and cutover. A successor may be forked after completed cutover, but
that technical ownership change invalidates package rollback and rollback then
returns `409` rather than overwriting the fork.

### Endpoint And Authorization Gates

The endpoint gate applies equally to Full Edge and Low-cost Controller
projections. The endpoint pointer is a fast projection, not sufficient evidence
by itself: Cloud also verifies the referenced deployment row and exact release
provenance. Missing, mismatched, Failed, or not-yet-Active deployment evidence
keeps cutover or rollback completion blocked; Cloud does not infer activation
from command delivery.

Upgrade preview derives `StoreId` and `KioskId` from the source installation.
The request cannot retarget an upgrade to another tenant scope. Authorization is
checked against that persisted owner.

Upgrade creates new Draft resources and a new release; it never mutates an
installed or active release. Recompose-after-authoring remains a separate
workflow and is not inferred from package upgrade.

## Related Docs

- [Production Package Installation Flow](PRODUCTION_PACKAGE_INSTALLATION_FLOW.md)
- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Management API Surface](../api/MANAGEMENT_API_SURFACE.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
