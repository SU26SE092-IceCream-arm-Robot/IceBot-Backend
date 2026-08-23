# EF Core Migration Workflow

## Search Keywords

`EF migration`, `PostgreSQL migration`, `backfill`, `upgrade baseline`,
`nullable unique index`, `check constraint`, `schema rollout`, `data repair`

## Purpose

This workflow keeps schema evolution deterministic across an empty database and
an existing PostgreSQL deployment. A migration changes schema and deterministic
data only; it is not a seed, startup repair, or external-integration workflow.

## Ownership Boundaries

| Concern | Owner |
| --- | --- |
| Model configuration and migration source | Infrastructure |
| Applying a migration | deployment migration job |
| Development/demo seed | explicit development bootstrap |
| Repairing inconsistent production data | explicit, audited repair command or one deterministic migration |
| Remote provider I/O | application/Infrastructure feature workflow, never a migration |

The API host does not apply migrations, seed production data, or silently repair
historical rows during startup.

## Before Generating A Migration

1. Freeze the final model, constraints, ownership, and backfill rule.
2. Inspect existing rows that the new rule affects and identify legitimate
   states versus duplicates or invalid legacy data.
3. Choose one deterministic outcome for every existing row. If more than one
   outcome is valid, stop and obtain an explicit data decision.
4. Design diagnostics that name conflicting IDs and values. Do not rely on
   `SingleOrDefault()` or a generic database exception as the only evidence.
5. Generate one migration after the model is stable. Do not create successive
   repair migrations for the same incomplete model change.

## PostgreSQL Constraints

- A unique index containing nullable columns does not make `NULL` values equal.
  Use a partial unique index or a normalized non-null scope key when uniqueness
  must include an optional scope.
- Use `CHECK` constraints for mutually exclusive or required scope columns.
- Add foreign keys for durable ownership and audit references unless an external
  identity is intentionally retained as an immutable snapshot.
- Use a database constraint for every invariant that must survive concurrent
  writers; application validation alone is not sufficient.
- Reference [Data Modeling Rules](DATA_MODELING_RULES.md) for soft-delete,
  index, snapshot, and JSONB rules.

## Migration Rules

- Keep the migration transaction-safe and bounded.
- Never call HTTP, object storage, payment, mail, MQTT, or other remote systems
  from a migration.
- Never depend on current wall-clock state unless the time is persisted as an
  intentional migration constant.
- Backfill only data that can be derived deterministically from persisted
  records. Otherwise fail with a clear diagnostic or run an explicit repair
  workflow before adding the enforcing constraint.
- Add data constraints only after the migration has reconciled every row they
  would reject.

## Verification Evidence

Before release, verify all applicable rows of this matrix:

| Scenario | Required evidence |
| --- | --- |
| Model snapshot | Design-time pending-model check reports no untracked change. |
| Empty database | All migrations apply successfully. |
| Production-like baseline | Upgrade applies successfully with representative existing data. |
| Deterministic backfill | Expected rows are changed once; conflicting rows produce actionable IDs. |
| Constraints | PostgreSQL accepts valid concurrent writes and rejects invalid duplicates/scopes. |
| Application behavior | Focused PostgreSQL integration tests cover the new invariant and replay/concurrency path. |
| Deployment | Migration job completes before the API image rolls out. |

Skipped Docker/PostgreSQL integration tests are missing evidence, not a passing
database verification result.

## Rollout And Recovery

Treat application rollback separately from schema rollback. A deployed schema
must remain compatible with the intended application rollback window, or the
release needs an explicit forward-fix plan. Do not run an unreviewed destructive
down migration against production data.

## Related Docs

- [Data Modeling Rules](DATA_MODELING_RULES.md)
- [Vertical Slice Review](../process/VERTICAL_SLICE_REVIEW.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Startup And Bootstrap Rules](../operations/STARTUP_AND_BOOTSTRAP_RULES.md)
