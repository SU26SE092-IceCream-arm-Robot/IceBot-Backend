# Vertical Slice Review

Use this process for backend work that crosses an API, job, event handler,
aggregate, persistence boundary, projection, or external dependency. The unit
of review is the workflow and its invariants, not an individual layer or file.

## Search Keywords

`vertical slice review`, `workflow invariant`, `failure scenario`, `failure path`,
`completion evidence`, `scope freeze`, `independent diff review`, `definition of done`,
`wide scan`, `horizontal audit`, `finding ledger`, `pattern scan`, `root cause`

## When To Use

Use the full gate when a change affects one or more of:

- public API or Edge/event contracts;
- lifecycle or aggregate transitions;
- tenant authorization;
- retried or concurrent commands;
- multiple database writes or external I/O;
- background reconciliation, cleanup, retention, or notification delivery.

For a local mechanical change, use only the applicable checks. Documentation-only
work does not require runtime evidence unless the user explicitly requests it.

## Audit And Remediation Modes

Keep broad inspection separate from implementation.

### Wide Scan

The purpose of a wide scan is coverage and anomaly discovery, not immediate
remediation. Freeze the repository baseline first, then inspect multiple slices
for weak points in:

- API and message contracts;
- validation, authorization, and tenant scope;
- transaction, lifecycle, concurrency, and idempotency behavior;
- mappings, database constraints, logging, and external dependencies;
- missing, misleading, or happy-path-only tests.

Do not interrupt the scan to repair each isolated finding. Record enough evidence
to triage it:

| Field | Required content |
| --- | --- |
| Finding ID | Stable identifier used through remediation |
| Baseline | Commit/worktree state inspected |
| Vertical slice and entry point | API, job, event, or command where it appears |
| Invariant | Expected rule that may be violated |
| Reproduction | Input, state, expected result, actual result, and reproducibility |
| Evidence | File/line, trace, query, test, or database evidence |
| Impact | Financial, physical, tenant, data, operational, or maintainability impact |
| Classification | Suspected, Confirmed, Design Debt, or Deferred |
| Pattern hint | Other slices or query shapes that may share the root cause |

Use impact-based priority:

- `P0`: money movement, physical action, tenant isolation, security, or data loss;
- `P1`: lifecycle corruption, duplicate effects, concurrency, or permanently stuck workflow;
- `P2`: contract mismatch, weak observability/recovery, or material maintenance debt;
- `P3`: naming, layout, or duplication without incorrect behavior.

Do not label a code smell as a confirmed bug without reproduction or a proven
invariant violation.

### Vertical Remediation

After triage, select one finding and freeze its complete affected slice. Trace
from the entry point through validation, authorization, application orchestration,
domain transition, persistence, external I/O, projection, retry, cleanup, and
tests. Determine where the violation begins and which boundary should have
prevented it before choosing patch, refactor, or rewrite.

Once the root cause is known, scan horizontally for the same pattern. For example,
if the root cause is an unscoped `GetById` store method, inspect all stores and
handlers using that query shape; do not repair only the endpoint that exposed it.
Create a shared abstraction only when the instances enforce the same invariant
and have the same ownership boundary.

The operating loop is:

```text
Freeze baseline
  -> wide scan without edits
  -> findings ledger
  -> confirm, classify, and prioritize
  -> select and freeze one vertical slice
  -> trace root cause and failure paths
  -> repair the owning boundary
  -> regression tests
  -> horizontal same-pattern scan
  -> independent final-diff review and preflight
  -> close with evidence or record residual risk
  -> return to the wide scan
```

## Required Sequence

This sequence applies after a finding has been selected for remediation:

1. Freeze the scope before editing.
2. Map the complete workflow from entry point to durable and external effects.
3. Define applicable invariants.
4. Write failure scenarios before changing code.
5. Implement the complete frozen slice in one coherent pass.
6. Run focused verification, then the repository preflight.
7. Review the final diff independently without expanding the architecture.
8. Report completion only when every applicable item has evidence.

Do not replace this sequence with a layer-by-layer review. A controller, handler,
or repository can be correct in isolation while its retry job, retention policy,
or projection still violates the same workflow rule.

## Workflow Map

Trace the smallest applicable form of this chain:

```text
API / GraphQL / Job / Event Ingest
  -> authentication, authorization, and tenant scope
  -> request validation and idempotency identity
  -> application orchestration
  -> aggregate transition and cross-context snapshots/IDs
  -> transaction, locking, and database constraints
  -> external I/O and committed-state boundary
  -> projection, notification, audit, and stock evidence
  -> retry, reconciliation, retention, cleanup, and diagnostics
```

Search every use of the affected status, identity key, failure code, predicate,
and serialized field. Shared behavior must have one typed owner; API handlers,
jobs, and stores must not independently invent equivalent rules.

## Invariant Matrix

Complete this matrix before implementation. Use `N/A` only with a concrete
reason.

| Area | Questions | Expected evidence |
| --- | --- | --- |
| Lifecycle | Which transitions are allowed, terminal, reversible, or forbidden? | Domain/policy unit tests |
| Tenancy | Which Organization/Store/Kiosk owns the resource? Can role and scope be combined across assignments? | Cross-tenant negative integration test |
| Idempotency | What is the identity? What does same-key/different-payload do? How long is evidence retained? | Duplicate and retry tests plus uniqueness/lock review |
| Concurrency | Which resource is locked? What happens on two simultaneous requests or allocators? | PostgreSQL concurrency test |
| Transaction | Which writes commit atomically? Is item-level partial success intentional? | Rollback or partial-failure integration test |
| External I/O | Does I/O happen before or after validation/commit? What compensates orphaned or ambiguous results? | Dependency failure and recovery test |
| Projection | Do read models, customer status, SignalR, audit, and stock evidence agree with source state? | Result/projection assertions |
| Retry | Which failures are retryable? Does cancellation stop immediately? Can retry duplicate physical or financial effects? | Retry exhaustion, cancellation, and duplicate tests |
| Background jobs | Can one poison item stop a bounded batch? Can an ineligible oldest item starve later work? | Poison-item and starvation tests |
| Retention | Can purge remove dedup, audit, or recovery evidence still needed by a live source workflow? | Retention integration test |
| Security | Are secrets/raw payloads excluded from normal responses and logs? | Contract/result assertions |
| Compatibility | Must an existing client/Edge contract remain compatible? | Contract test or explicit pre-deployment `N/A` |

## Failure Scenario Worksheet

Record concrete scenarios, not generic labels. At minimum consider:

- duplicate request before and after the original response is lost;
- same idempotency key with a different payload;
- two backend instances processing the same source identity;
- process crash before commit, after commit, or during external I/O;
- database success with notification/object-storage/provider failure;
- external success with database failure;
- stale state observed by a job after a newer transition;
- oldest batch candidate is invalid, has no recipient, or always throws;
- retry after retention or cleanup;
- cancellation while waiting or retrying;
- cross-tenant ID supplied to an otherwise authorized actor;
- physical output or money movement may already have occurred.

For each applicable scenario, state the expected durable state, retry behavior,
operator visibility, and whether automatic recovery is allowed.

## Temporary Review Record

For a substantial active task, create a short-lived worksheet in
`.project-memory/<SLICE_NAME>_REVIEW.md` using this structure:

```markdown
# <Slice Name> Review

## Frozen Scope
- Included:
- Excluded:
- Public contract changes:
- EF model/migration allowed:

## Workflow Map
- Entry point:
- Authority/aggregate:
- Persistence and locks:
- External effects:
- Reconciliation/retention:

## Invariants And Evidence
| Area | Invariant or N/A reason | Verification |
| --- | --- | --- |

## Failure Scenarios
| Scenario | Expected result | Verification |
| --- | --- | --- |

## Final Diff Review
- Stale/duplicated rules searched:
- Changes outside scope:
- Unverified risks:
```

Delete the temporary worksheet when the task is complete. Promote changed
contracts to their owning backend document and design reasoning to `Vault/`.

## Verification Order

Use the narrowest check that proves each invariant, then broaden:

1. policy/domain unit tests;
2. focused PostgreSQL/MinIO/provider integration tests;
3. concurrency and failure-path tests;
4. full affected test project;
5. backend preflight;
6. EF pending-model check when persistence mapping may have changed;
7. `git diff --check`, stale-identifier search, and independent final diff review.

Build success proves compilation only. A happy-path test does not prove retry,
tenancy, concurrency, cleanup, or retention behavior.

## Completion Report

The final report must distinguish:

- implemented behavior;
- verification evidence with actual pass counts;
- checks not run and why;
- residual risks or scenarios without evidence;
- whether an EF migration was created or required.

Do not state that the slice is complete while a material applicable failure path
has no evidence. State the missing evidence directly instead.

## Related Docs

- [Working Protocol](WORKING_PROTOCOL.md)
- [Backend Critical Rule Checklist](BACKEND_CRITICAL_RULE_CHECKLIST.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [Idempotency And Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
